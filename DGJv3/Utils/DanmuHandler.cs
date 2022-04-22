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

        /// <summary>
        /// 需要房管权限的指令清单
        /// </summary>
        public string AdminCommand
        {
            get => string.Join(JOIN_STRING, _admminCommand);
            set
            {
                if (value == null) value = "";
                else
                {
                    Regex regClean = new Regex(@"[\s,，;；]{1,}", RegexOptions.IgnoreCase);
                    value = regClean.Replace(value, JOIN_STRING).Trim();
                }
                SetField(ref _admminCommand, value);
                _adminCommandType = (from str in value.Split(JOIN_STRING.ToCharArray()) select GetCommandType(str)).ToArray();
            }
        }
        private string _admminCommand;

        private CommandType[] _adminCommandType;

        private bool IsAdminCommand(CommandType commandType)
        {
            return _adminCommandType.Contains(commandType);
        }


        /// <summary>
        /// 投票切歌的人数
        /// </summary>
        public int Vote4NextCount { get => _vote4NextCount < 1 ? 1 : _vote4NextCount; set => SetField(ref _vote4NextCount, value); }
        private int _vote4NextCount;

        private ICollection<int> vote4NextUserCache = new List<int>();

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

            string[] commands = danmakuModel.CommentText.Split(JOIN_STRING.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string rest = string.Join(JOIN_STRING, commands.Skip(1));

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
                            if (danmakuModel.isAdmin)
                            {
                                Player.Next();
                                return;
                            }
                            Vote4Next(danmakuModel.UserID);
                        });
                    }
                    return;
                case CommandType.Skip:
                    {
                        dispatcher.Invoke(() =>
                        {
                            Player.Next();
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
                        if (commands.Length == 1)
                        {
                            Log($"当前音量：{Player.Volume}");
                            return;
                        }
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

        /// <summary>
        /// 将驶入指令翻译为枚举类型
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private CommandType GetCommandType(string key)
        {
            switch (key)
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
                case "下一首":
                    return CommandType.Next;
                case "切歌":
                    return CommandType.Skip;
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

        /// <summary>
        /// 判断用户指令是否无权限
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="danmakuModel"></param>
        /// <returns></returns>
        private bool NoCommandRight(CommandType commandType, DanmakuModel danmakuModel)
        {
            return commandType == CommandType.Null || (IsAdminCommand(commandType) && !danmakuModel.isAdmin);
        }

        /// <summary>
        /// 通过弹幕点歌
        /// </summary>
        /// <param name="danmakuModel"></param>
        /// <param name="keyword"></param>
        private void DanmuAddSong(DanmakuModel danmakuModel, string keyword)
        {
            if (dispatcher.Invoke(callback: () => CanAddSong(username: danmakuModel.UserName)))
            {
                SongInfo songInfo = SearchModules.GetSongInfo(keyword);

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

        /// <summary>
        /// 添加歌曲
        /// </summary>
        /// <param name="songInfo"></param>
        /// <param name="userName"></param>
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

            //空闲歌单曲目后置（删除后重新加入）
            var pending = Songs.Where(s => s.UserName == Utilities.SparePlaylistUser && s.Status != SongStatus.Playing).ToArray();
            if (pending.Length > 0)
            {
                foreach (var songItem in pending)
                {
                    Songs.Remove(songItem);
                    Songs.Add(songItem);
                }
            }
        }

        /// <summary>
        /// 投票切歌
        /// </summary>
        /// <param name="userId"></param>
        private void Vote4Next(int userId)
        {
            if (vote4NextUserCache.Contains(userId))
            {
                return;
            }
            vote4NextUserCache.Add(userId);
            if (vote4NextUserCache.Count >= Vote4NextCount)
            {
                Player.Next();
                Log("投票通过，切歌至下一首！");
            }
            else
            {
                Log($"切歌投票：{vote4NextUserCache.Count}/{Vote4NextCount}");
            }
        }
        /// <summary>
        /// 清空投票切歌的缓存
        /// </summary>
        public void CLearVote4NextCache()
        {
            if (vote4NextUserCache.Count == 0) return;
            vote4NextUserCache.Clear();
        }

        /// <summary>
        /// 卸载歌曲后的处理工作
        /// </summary>
        public void AfterUnloadSong()
        {
            //TrySortSongs();
            CLearVote4NextCache();
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

        public static readonly string JOIN_STRING = " ";

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
