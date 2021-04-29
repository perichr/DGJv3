using BilibiliDM_PluginFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using System.Text.RegularExpressions;

namespace DGJv3
{
    class DanmuHandler : INotifyPropertyChanged
    {
        private ObservableCollection<SongItem> Songs;

        private ObservableCollection<BlackListItem> Blacklist;

        private Player Player;

        private Downloader Downloader;

        private SearchModules SearchModules;

        private Dispatcher dispatcher;
        private History history { get; set; }

        /// <summary>
        /// 最多点歌数量
        /// </summary>
        public uint MaxTotalSongNum { get => _maxTotalSongCount; set => SetField(ref _maxTotalSongCount, value); }
        private uint _maxTotalSongCount;

        /// <summary>
        /// 每个人最多点歌数量
        /// </summary>
        public uint MaxPersonSongNum { get => _maxPersonSongNum; set => SetField(ref _maxPersonSongNum, value); }
        private uint _maxPersonSongNum;

        /// <summary>
        /// 允许取消正在播放的歌曲
        /// </summary>
        public bool IsAllowCancelPlayingSong { get => _isAllowCancelPlayingSong; set => SetField(ref _isAllowCancelPlayingSong, value); }
        private bool _isAllowCancelPlayingSong;

        public string AdminCommand
        {
            get => _admminCommand;
            set
            {
                SetField(ref _admminCommand, value);

                adminCommand = new Dictionary<CommandType, bool>();
                var ss = value.Replace(',', ' ').Replace('，', ' ').Split(' ');
                foreach (CommandType item in Enum.GetValues(typeof(CommandType))) adminCommand.Add(item, false);
                foreach (var item in ss)
                {
                    var ct = GetCommandType(item);
                    if (ct == CommandType.Null) continue;
                    adminCommand[ct] = true;
                }
            }
        }
        private string _admminCommand;
        private Dictionary<CommandType, bool> adminCommand;



        internal DanmuHandler(ObservableCollection<SongItem> songs, Player player, Downloader downloader, SearchModules searchModules, ObservableCollection<BlackListItem> blacklist)
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            Songs = songs;
            Player = player;
            Player.DanmuHandler = this;
            Downloader = downloader;
            SearchModules = searchModules;
            Blacklist = blacklist;
            history = new History();

        }

        /// <summary>
        /// 处理弹幕消息
        /// <para>
        /// 注：调用侧可能会在任意线程
        /// </para>
        /// </summary>
        /// <param name="danmakuModel"></param>
        internal void ProcessDanmu(DanmakuModel danmakuModel)
        {
            if (danmakuModel.MsgType != MsgTypeEnum.Comment || string.IsNullOrWhiteSpace(danmakuModel.CommentText))
                return;

            string[] commands = danmakuModel.CommentText.Split(SPLIT_CHAR, StringSplitOptions.RemoveEmptyEntries);
            string rest = string.Join(" ", commands.Skip(1));

            CommandType commandType = GetCommandType(commands[0]);

            if (NoCommandRight(commandType, danmakuModel)) return;

            switch (commandType)
            {
                case CommandType.Add:
                    {
                        DanmuAddSong(danmakuModel, rest);
                    }
                    return;
                case CommandType.Cancel:
                    {
                        dispatcher.Invoke(() =>
                        {
                            SongItem songItem = Songs.LastOrDefault(x => x.UserName == danmakuModel.UserName && (IsAllowCancelPlayingSong || x.Status != SongStatus.Playing));
                            RemoveSong(songItem);
                        });
                    }
                    return;
                case CommandType.AddLast:
                    {
                        dispatcher.Invoke(() =>
                        {
                            if (Player.LastSongInfo == null) Log("没有上一首歌曲的信息！");
                            else AddSong(Player.LastSongInfo, danmakuModel.UserName);
                        });
                    }
                    return;
                case CommandType.AddCurent:
                    {
                        dispatcher.Invoke(() =>
                        {
                            if (Player.CurrentSong == null) Log("没有正在播放的歌曲！");
                            else AddSong(Player.CurrentSong.Info, danmakuModel.UserName);
                        });
                    }
                    return;
                case CommandType.Info:
                    {
                        dispatcher.Invoke(() =>
                        {
                            if (Player.CurrentSong == null) Log("没有正在播放的歌曲！");
                            else Log("正在播放：" + Player.CurrentSong.ModuleName + ":" + Player.CurrentSong.SongId);
                        });
                    }
                    return;
                case CommandType.Next:
                    {
                        dispatcher.Invoke(() =>
                        {
                            RemoveSong(0);
                            Log("切歌成功！");

                            // 切至指定序号的歌曲
                            if (commands.Length > 1
                               && int.TryParse(rest, out int i)
                               && i > 1
                               && Songs.Count >= i
                               )
                            {
                                i--;
                                SongItem si = Songs[i];
                                Songs.RemoveAt(i);
                                Songs.Insert(0, si);
                            }
                        });
                    }
                    return;
                case CommandType.Pause:
                    {
                        Player.Pause();
                    }
                    return;
                case CommandType.Play:
                    {
                        Player.Play();
                    }
                    return;
                case CommandType.Volume:
                    {
                        if (commands.Length > 1
                            && int.TryParse(commands[1], out int volume100)
                            && volume100 >= 0
                            && volume100 <= 100)
                        {
                            Player.Volume = volume100 / 100f;
                        }
                    }
                    return;
                default:
                    break;
            }
        }

        private CommandType GetCommandType(string name)
        {
            switch (name)
            {
                case "点歌":
                case "點歌":
                    return CommandType.Add;
                case "取消點歌":
                case "取消点歌":
                    return CommandType.Cancel;
                case "上一首":
                    return CommandType.AddLast;
                case "重播":
                    return CommandType.AddCurent;
                case "信息":
                    return CommandType.Info;
                case "切歌":
                    return CommandType.Next;
                case "暂停":
                case "暫停":
                    return CommandType.Pause;
                case "播放":
                    return CommandType.Play;
                case "音量":
                    return CommandType.Volume;
            }
            return CommandType.Null;
        }

        private bool NoCommandRight(CommandType commandType, DanmakuModel danmakuModel)
        {
            return commandType == CommandType.Null || (adminCommand[commandType] && !danmakuModel.isAdmin);
        }

        private void DanmuAddSong(DanmakuModel danmakuModel, string keyword)
        {
            if (dispatcher.Invoke(callback: () => CanAddSong(username: danmakuModel.UserName)))
            {
                SongInfo songInfo = null;

                if (SearchModules.PrimaryModule != SearchModules.NullModule)
                    songInfo = SearchModules.PrimaryModule.SafeSearch(keyword);

                if (songInfo == null)
                    if (SearchModules.SecondaryModule != SearchModules.NullModule)
                        songInfo = SearchModules.SecondaryModule.SafeSearch(keyword);

                if (songInfo == null)
                    return;

                if (songInfo.IsInBlacklist(Blacklist))
                {
                    Log($"歌曲在黑名单中：{songInfo.Name}");
                    return;
                }
                Log($"点歌成功:{songInfo.Name}");
                history.Write(songInfo, danmakuModel.UserName);
                dispatcher.Invoke(callback: () =>
                {
                    if (CanAddSong(danmakuModel.UserName) &&
                        !Songs.Any(x =>
                            x.SongId == songInfo.Id &&
                            x.Module.UniqueId == songInfo.Module.UniqueId)
                    )
                        AddSong(songInfo, danmakuModel.UserName);
                });
            }
        }

        public void AddSong(SongInfo songInfo, string userName)
        {
            Songs.Add(new SongItem(songInfo, userName));
            TrySortSongs();
        }

        /// <summary>
        /// 用户点歌优先时，尝试调序
        /// </summary>
        public void TrySortSongs()
        {
            //非用户点歌优先时跳过
            if (!Player.IsUserPrior) return;

            //播放列表小于2条时跳过
            if (Songs.Count < 2) return;

            //删除播放列表全部空闲歌单曲目   
            var pending = Songs.Where(s => s.UserName == Utilities.SparePlaylistUser).ToArray();
            foreach (var songItem in pending) RemoveSong(songItem);
        }





        /// <summary>
        /// 能否点歌
        /// <para>
        /// 注：调用侧需要在主线程上运行
        /// </para>
        /// </summary>
        /// <param name="username">点歌用户名</param>
        /// <returns></returns>
        private bool CanAddSong(string username)
        {
            return Songs.Count < MaxTotalSongNum && (Songs.Where(x => x.UserName == username).Count() < MaxPersonSongNum);
        }

        private static readonly char[] SPLIT_CHAR = { ' ' };

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public event LogEvent LogEvent;
        private void Log(string message, Exception exception = null) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });


        private void RemoveSong(int i)
        {
            if (Songs.Count > i)
            {
                RemoveSong(Songs[i]);
            }
        }

        private void RemoveSong(SongItem songItem)
        {
            if (songItem != null)
            {
                songItem.Remove(Songs, Downloader, Player);
            }
        }
    }
}
