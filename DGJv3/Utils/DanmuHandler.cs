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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DGJv3
{
    class DanmuHandler : INotifyPropertyChanged
    {
        private ObservableCollection<SongItem> Songs;

        private ObservableCollection<SongInfo> Playlist;

        private ObservableCollection<BlackListItem> Blacklist;

        private Player Player;

        private Downloader Downloader;

        private SearchModules SearchModules;

        private Dispatcher dispatcher;
        private History history { get; set; }



        private ICollection<string> vote4NextUserCache = new List<string>();

        internal DanmuHandler(ObservableCollection<SongItem> songs, Player player, ObservableCollection<SongInfo> playlist,Downloader downloader, SearchModules searchModules, ObservableCollection<BlackListItem> blacklist)
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            Songs = songs;
            Player = player;
            Player.DanmuHandler = this;
            Playlist = playlist;
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

            string[] commands = danmakuModel.CommentText.Split(Utilities.JOIN_STRING.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string rest = string.Join(Utilities.JOIN_STRING, commands.Skip(1));

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
                            SongItem songItem = Songs.LastOrDefault(x => x.IsAddedByUser(danmakuModel) && (Config.Current.IsAllowCancelPlayingSong || (!x.IsPlaying)));
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
                            if (IsAdminUser(danmakuModel) || Player.CurrentSong.IsAddedByUser(danmakuModel))
                            {
                                Player.Next();
                                return;
                            }
                            Vote4Next(danmakuModel.UserName);
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
                case CommandType.ListAdd:
                    if (Player.CurrentSong.UserName != Utilities.SparePlaylistUser)
                    {
                        Playlist.Add(Player.CurrentSong.Info);
                    }
                    return;
                case CommandType.ListDel:
                    if (Player.CurrentSong.UserName == Utilities.SparePlaylistUser)
                    {
                        Playlist.Remove(Player.CurrentSong.Info);
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
        public static CommandType GetCommandType(string key)
        {
            switch (key)
            {
                case "Add":
                case "点歌":
                case "點歌":
                    return CommandType.Add;
                case "Cancel":
                case "取消點歌":
                case "取消点歌":
                    return CommandType.Cancel;
                case "AddLast":
                case "上一首":
                    return CommandType.AddLast;
                case "AddCurent":
                case "重播":
                    return CommandType.AddCurent;
                case "Info":
                case "信息":
                    return CommandType.Info;
                case "Next":
                case "下一首":
                    return CommandType.Next;
                case "Skip":
                case "切歌":
                    return CommandType.Skip;
                case "Pause":
                case "暂停":
                case "暫停":
                    return CommandType.Pause;
                case "Play":
                case "播放":
                    return CommandType.Play;
                case "Volume":
                case "音量":
                    return CommandType.Volume;
                case "ListAdd":
                case "添加":
                    return CommandType.ListAdd;
                case "ListADel":
                case "删除":
                    return CommandType.ListDel;
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
            return commandType == CommandType.Null || (IsAdminCommand(commandType) && !IsAdminUser(danmakuModel));
        }

        public bool IsAdminCommand(CommandType commandType)
        {
            return (from str in Config.Current.AdminCommand.Split(Utilities.JOIN_STRING.ToCharArray()) select GetCommandType(str)).Contains(commandType);
        }

        private bool IsAdminUser(DanmakuModel model)
        {
            return Config.Current.AdminList.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Contains(model.UserName);
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
            if (!Config.Current.IsUserPrior) return;

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
        private void Vote4Next(string name)
        {
            if (!vote4NextUserCache.Contains(name))
            {
                vote4NextUserCache.Add(name);
            }
            if (vote4NextUserCache.Count < Config.Current.Vote4NextCount)
            {
                Log($"切歌投票：{vote4NextUserCache.Count}/{Config.Current.Vote4NextCount}");
            }
            else
            {
                Player.Next();
                Log("投票通过，切歌至下一首！");
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
            return Songs.Count < Config.Current.MaxTotalSongNum && (Songs.Where(x => x.UserName == username).Count() < Config.Current.MaxPersonSongNum);
        }


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
            songItem?.Remove(Songs, Downloader, Player);
        }
    }
}
