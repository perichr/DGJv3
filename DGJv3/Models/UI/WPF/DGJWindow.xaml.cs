using DGJv3.InternalModule;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Text.RegularExpressions;
using System.Text;

namespace DGJv3
{
    /// <summary>
    /// DGJWindow.xaml 的交互逻辑
    /// </summary>
    internal partial class DGJWindow : Window
    {
        public DGJMain PluginMain { get; set; }

        public ObservableCollection<SongItem> Songs { get; set; }

        public ObservableCollection<SongInfo> Playlist { get; set; }

        public ObservableCollection<BlackListItem> Blacklist { get; set; }

        public ObservableCollection<string> ConfigBackupList { get; set; }

        public Player Player { get; set; }

        public Downloader Downloader { get; set; }

        public Writer Writer { get; set; }

        public SearchModules SearchModules { get; set; }

        public DanmuHandler DanmuHandler { get; set; }

        public UniversalCommand RemoveSongCommand { get; set; }

        public UniversalCommand RemoveAndBlacklistSongCommand { get; set; }

        public UniversalCommand RemovePlaylistInfoCommand { get; set; }

        public UniversalCommand ClearPlaylistCommand { get; set; }

        public UniversalCommand RemoveBlacklistInfoCommand { get; set; }

        public UniversalCommand ClearBlacklistCommand { get; set; }

        public UniversalCommand RemoveUsingModulesCommand { get; set; }

        public UniversalCommand AddUsingModulesCommand { get; set; }

        public UniversalCommand MoveUpUsingModuleCommand { get; set; }

        public UniversalCommand MoveDownUsingModuleCommand { get; set; }

        public UniversalCommand AddToPlayListCommand { get; set; }

        public UniversalCommand BackupConfigCommand { get; set; }

        public UniversalCommand LoadCurrentConfigCommand { get; set; }

        public UniversalCommand LoadConfigCommand { get; set; }

        public UniversalCommand RemoveConfigCommand { get; set; }




        public bool IsLogRedirectDanmaku { get; set; }

        public int LogDanmakuLengthLimit { get; set; }

        private bool ApplyConfigReady = false;

        public void Log(string text)
        {
            PluginMain.Log(text);
            WriteLog(text);
            if (IsLogRedirectDanmaku)
            {
                SendMessage(text);
            }
        }
        private static readonly Queue<string> _log = new Queue<string>();
        private void WriteLog(string text)
        {
            SendDanmaku.FilterMessage(ref text, false);
            if (string.IsNullOrWhiteSpace(text)) return;

            if (_log.Count == 20) //临时指定20个指令。后续再作优化，
            {
                _log.Dequeue();
            }
            _log.Enqueue(DateTime.Now.ToString("[MM/dd hh:mm:ss]") + text);
            File.WriteAllText(Utilities.LogOutputFilePath, string.Join<string>(Environment.NewLine, _log.ToArray().Reverse()), Encoding.UTF8);
        }


        private static readonly Queue<string> danmuCache = new Queue<string>();
        private static string danmuCacheOne;

        private static readonly DispatcherTimer senDanmuTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromSeconds(2),
            IsEnabled = true,
        };

        /// <summary>
        /// 循环发送弹幕。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendDanmuTimer_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(danmuCacheOne))
            {
                SendDanmu(danmuCacheOne);
                danmuCacheOne = null;
            }
            if (danmuCache.Count == 0)
            {
                senDanmuTimer.Stop();
                return;
            };
            if (danmuCache.Count == 2) danmuCache.TrimExcess();
            danmuCacheOne = danmuCache.Dequeue();
        }

        /// <summary>
        /// 将长字符串拆解为不多于3段并写入缓存。
        /// </summary>
        /// <param name="text"></param>
        private void SendMessage(string text)
        {
            if (!PluginMain.RoomId.HasValue) return;
            SendDanmaku.FilterMessage(ref text);
            if (string.IsNullOrWhiteSpace(text)) return;
            if (!senDanmuTimer.IsEnabled) senDanmuTimer.Start();
            int max = 4;
            int times = 0;
            int pos = 0;
            do
            {
                int len = Math.Min(text.Length - pos, LogDanmakuLengthLimit);
                string value;
                if (times == max - 1 && LogDanmakuLengthLimit - len < 3)
                {
                    value = text.Substring(pos, LogDanmakuLengthLimit - 3) + "...";
                }
                else
                {
                    value = text.Substring(pos, len);
                }
                danmuCache.Enqueue(value);
                times++;
                pos += LogDanmakuLengthLimit;
            }
            while (times < max && pos < text.Length);
        }

        /// <summary>
        /// 直接调用发送弹幕。
        /// </summary>
        /// <param name="text"></param>
        private void SendDanmu(string text)
        {
            Task.Run(async () =>
            {
                try
                {
                    string result = await SendDanmaku.SendDanmakuAsync(PluginMain.RoomId.Value, text, LoginCenter.API.LoginCenterAPI.getCookies());
                    if (result == null)
                    {
                        PluginMain.Log("发送弹幕时网络错误");
                    }
                    else
                    {
                        var j = JObject.Parse(result);
                        if (j["msg"].ToString() != string.Empty)
                        {
                            PluginMain.Log("发送弹幕时服务器返回：" + j["msg"].ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex.GetType().FullName.Equals("LoginCenter.API.PluginNotAuthorizedException"))
                    {
                        IsLogRedirectDanmaku = false;
                    }
                    else
                    {
                        PluginMain.Log("弹幕发送错误 " + ex.ToString());
                    }
                }
            });
        }


        public DGJWindow(DGJMain dGJMain)
        {
            void addResource(string uri)
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(uri)
                });
            }
            addResource("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml");
            addResource("pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Blue.xaml");
            addResource("pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.DeepOrange.xaml");
            addResource("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.ProgressBar.xaml");

            DataContext = this;
            PluginMain = dGJMain;
            Songs = new ObservableCollection<SongItem>();
            Playlist = new ObservableCollection<SongInfo>();
            Blacklist = new ObservableCollection<BlackListItem>();
            ConfigBackupList = new ObservableCollection<string>();

            Player = new Player(Songs, Playlist);
            Downloader = new Downloader(Songs);
            SearchModules = new SearchModules();
            DanmuHandler = new DanmuHandler(Songs, Player, Downloader, SearchModules, Blacklist);
            Writer = new Writer(Songs, Playlist, Player, DanmuHandler);

            Player.LogEvent += (sender, e) => { Log("播放:" + e.ToString()); };
            Downloader.LogEvent += (sender, e) => { Log("下载:" + e.ToString()); };
            Writer.LogEvent += (sender, e) => { Log("文本:" + e.ToString()); };
            SearchModules.LogEvent += (sender, e) => { Log("搜索:" + e.ToString()); };
            DanmuHandler.LogEvent += (sender, e) => { Log("" + e.ToString()); };

            RemoveSongCommand = new UniversalCommand((songobj) =>
            {
                if (songobj != null && songobj is SongItem songItem)
                {
                    songItem.Remove(Songs, Downloader, Player);
                }
            });

            RemoveAndBlacklistSongCommand = new UniversalCommand((songobj) =>
            {
                if (songobj != null && songobj is SongItem songItem)
                {
                    songItem.Remove(Songs, Downloader, Player);
                    Blacklist.Add(new BlackListItem(BlackListType.Id, songItem.SongId));
                }
            });

            RemovePlaylistInfoCommand = new UniversalCommand((songobj) =>
            {
                if (songobj != null && songobj is SongInfo songInfo)
                {
                    Playlist.Remove(songInfo);
                }
            });

            ClearPlaylistCommand = new UniversalCommand((e) =>
            {
                Playlist.Clear();
            });

            RemoveBlacklistInfoCommand = new UniversalCommand((blackobj) =>
            {
                if (blackobj != null && blackobj is BlackListItem blackListItem)
                {
                    Blacklist.Remove(blackListItem);
                }
            });


            ClearBlacklistCommand = new UniversalCommand((x) =>
            {
                Blacklist.Clear();
            });

            RemoveUsingModulesCommand = new UniversalCommand((smobj) =>
            {
                if (smobj != null && smobj is SearchModule searchModule)
                {
                    SearchModules.UsingModules.Remove(searchModule);
                }
            });
            AddUsingModulesCommand = new UniversalCommand((smobj) =>
            {
                if (smobj == null || !(smobj is SearchModule searchModule) || SearchModules.UsingModules.Contains(searchModule))
                {
                    return;
                }
                SearchModules.UsingModules.Add(searchModule);
            });

            MoveUpUsingModuleCommand = new UniversalCommand((smobj) =>
            {
                if (smobj == null || !(smobj is SearchModule searchModule))
                {
                    return;
                }
                SearchModules.MoveUsingModule(searchModule, true);
            });
            MoveDownUsingModuleCommand = new UniversalCommand((smobj) =>
            {
                if (smobj == null || !(smobj is SearchModule searchModule))
                {
                    return;
                }
                SearchModules.MoveUsingModule(searchModule, false);
            });

            AddToPlayListCommand = new UniversalCommand((songobj) =>
            {
                if (songobj != null && songobj is SongItem songItem && songItem.UserName != Utilities.SparePlaylistUser)
                {
                    AddToPlayList(songItem.Info);
                }
            });

            BackupConfigCommand = new UniversalCommand((x) =>
            {
                SaveConfig(true);
                TryGetConfigBackupLst();
            });

            LoadCurrentConfigCommand = new UniversalCommand((x) =>
            {
                TryApplyConfig(null,true);
            });

            RemoveConfigCommand = new UniversalCommand((obj) =>
            {
                if (obj != null && obj is string str)
                {
                    try
                    {
                        File.Delete(Config.GetConfigPath(str));
                    }
                    catch { }
                    TryGetConfigBackupLst();
                }
            });

            LoadConfigCommand = new UniversalCommand((obj) =>
             {
                 if (obj != null && obj is string str)
                 {
                     TryApplyConfig(Config.GetConfigPath(str), true);
                 }
             });


            InitializeComponent();

            PluginMain.ReceivedDanmaku += (sender, e) => { DanmuHandler.ProcessDanmu(e.Danmaku); };
            PluginMain.Connected += (sender, e) => { ApiBaseModule.RoomId = e.roomid; };
            PluginMain.Disconnected += (sender, e) => { ApiBaseModule.RoomId = 0; };

            senDanmuTimer.Tick += SendDanmuTimer_Tick;


            TryGetConfigBackupLst();

        }

        void AddToPlayList(SongInfo songInfo)
        {
            if (songInfo != null)
            {
                if (Playlist.FirstOrDefault(x => (x.Id == songInfo.Id && x.ModuleId == songInfo.ModuleId)) == null)
                {
                    Playlist.Add(songInfo);
                }
            }
        }

        public void TryApplyConfig(string path = null, bool force = false)
        {
            if (!force && ApplyConfigReady)
                return;
            ApplyConfig(Config.Load(path));
        }

        public void TryGetConfigBackupLst()
        {
            ConfigBackupList.Clear();

            string[] list = Directory.GetFiles(Utilities.ConfigBackupDirectoryPath, "config.??????????????.json");
            if (list.Length > 0)
            {
                Array.Sort(list);
                foreach (string str in list)
                {
                    ConfigBackupList.Add(Path.GetFileNameWithoutExtension(str).Substring(7));
                }
            }
        }
        /// <summary>
        /// 应用设置
        /// </summary>
        /// <param name="config"></param>
        private void ApplyConfig(Config config)
        {
            LogRedirectToggleButton.IsEnabled = LoginCenterAPIWarpper.CheckLoginCenter();

            Player.PlayerType = config.PlayerType;
            Player.DirectSoundDevice = config.DirectSoundDevice;
            Player.WaveoutEventDevice = config.WaveoutEventDevice;
            Player.Volume = config.Volume;
            Player.IsUserPrior = config.IsUserPrior;
            Player.IsPlaylistEnabled = config.IsPlaylistEnabled;
            Player.MaxPlayTime = config.MaxPlayTime;
            DanmuHandler.IsAllowCancelPlayingSong = config.IsAllowCancelPlayingSong;
            DanmuHandler.MaxTotalSongNum = config.MaxTotalSongNum;
            DanmuHandler.MaxPersonSongNum = config.MaxPersonSongNum;
            DanmuHandler.AdminCommand = config.AdminCommand;
            DanmuHandler.AdminList = config.AdminList;
            DanmuHandler.Vote4NextCount = config.Vote4NextCount;
            Writer.ScribanTemplate = config.ScribanTemplate;
            IsLogRedirectDanmaku = LogRedirectToggleButton.IsEnabled && config.IsLogRedirectDanmaku;
            LogDanmakuLengthLimit = config.LogDanmakuLengthLimit;


            SearchModules.UsingModules.Clear();
            foreach (var item in config.UsingModules)
            {
                var sm = SearchModules.Modules.FirstOrDefault(x => x.UniqueId == item);
                if (sm == null || SearchModules.UsingModules.Contains(sm))
                {
                    continue;
                }
                SearchModules.UsingModules.Add(sm);
            }
            if(SearchModules.UsingModules.Count == 0)
            {
                SearchModules.UsingModules.Add(SearchModules.Modules[0]);
                SearchModules.UsingModules.Add(SearchModules.Modules[1]);
            }

            Playlist.Clear();
            foreach (var item in config.Playlist)
            {
                item.Module = SearchModules.Modules.FirstOrDefault(x => x.UniqueId == item.ModuleId);
                if (item.Module != null)
                {
                    Playlist.Add(item);
                }
            }

            Blacklist.Clear();
            foreach (var item in config.Blacklist)
            {
                Blacklist.Add(item);
            }
            ApplyConfigReady = true;
        }

        /// <summary>
        /// 收集设置
        /// </summary>
        /// <returns></returns>
        private Config GatherConfig() => new Config()
        {
            PlayerType = Player.PlayerType,
            DirectSoundDevice = Player.DirectSoundDevice,
            WaveoutEventDevice = Player.WaveoutEventDevice,
            IsUserPrior = Player.IsUserPrior,
            IsAllowCancelPlayingSong = DanmuHandler.IsAllowCancelPlayingSong,
            Volume = Player.Volume,
            IsPlaylistEnabled = Player.IsPlaylistEnabled,
            MaxPlayTime = Player.MaxPlayTime,
            MaxPersonSongNum = DanmuHandler.MaxPersonSongNum,
            MaxTotalSongNum = DanmuHandler.MaxTotalSongNum,
            AdminCommand = DanmuHandler.AdminCommand,
            AdminList = DanmuHandler.AdminList,
            Vote4NextCount = DanmuHandler.Vote4NextCount,
            ScribanTemplate = Writer.ScribanTemplate,
            Playlist = Playlist.ToArray(),
            Blacklist = Blacklist.ToArray(),
            IsLogRedirectDanmaku = IsLogRedirectDanmaku,
            LogDanmakuLengthLimit = LogDanmakuLengthLimit,
            UsingModules = (from sm in SearchModules.UsingModules select sm.UniqueId).ToArray(),
        };

        public void SaveConfig(bool backup = false)
        {
            Config.Write(GatherConfig(), backup);
        }

        /// <summary>
        /// 弹幕姬退出事件
        /// </summary>
        internal void DeInit()
        {
            SaveConfig();
            Downloader.CancelDownload();
            Player.Next();
            try
            {
                Directory.Delete(Utilities.SongsCacheDirectoryPath, true);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 主界面右侧
        /// 添加歌曲的
        /// dialog 的
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void DialogAddSongs(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter.Equals(true) && !string.IsNullOrWhiteSpace(AddSongsTextBox.Text))
            {
                SongInfo songInfo = SearchModules.GetSongInfo(AddSongsTextBox.Text);
                if (songInfo == null)
                {
                    return;
                }
                DanmuHandler.AddSong(songInfo, Utilities.AnchorName);
            }
            AddSongsTextBox.Text = string.Empty;
        }

        /// <summary>
        /// 主界面右侧
        /// 添加空闲歌曲按钮的
        /// dialog 的
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void DialogAddSongsToPlaylist(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter.Equals(true) && !string.IsNullOrWhiteSpace(AddSongPlaylistTextBox.Text))
            {
                SongInfo songInfo = SearchModules.GetSongInfo(AddSongPlaylistTextBox.Text);
                if (songInfo == null)
                {
                    return;
                }
                AddToPlayList(songInfo);
            }
            AddSongPlaylistTextBox.Text = string.Empty;
        }

        /// <summary>
        /// 主界面右侧
        /// 添加空闲歌单按钮的
        /// dialog 的
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void DialogAddPlaylist(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter.Equals(true) && !string.IsNullOrWhiteSpace(AddPlaylistTextBox.Text))
            {
                List<SongInfo> songInfoList =SearchModules.GetSongInfoList(AddPlaylistTextBox.Text);

                if (songInfoList == null)
                {
                    return;
                }

                foreach (var item in songInfoList)
                {
                    Playlist.Add(item);
                }
            }
            AddPlaylistTextBox.Text = string.Empty;
        }

        /// <summary>
        /// 黑名单 popupbox 里的
        /// 添加黑名单按钮的
        /// dialog 的
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void DialogAddBlacklist(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter.Equals(true)
                && !string.IsNullOrWhiteSpace(AddBlacklistTextBox.Text)
                && AddBlacklistComboBox.SelectedValue != null
                && AddBlacklistComboBox.SelectedValue is BlackListType type1)
            {
                var keyword = AddBlacklistTextBox.Text;
                var type = type1;

                Blacklist.Add(new BlackListItem(type, keyword));
            }
            AddBlacklistTextBox.Text = string.Empty;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private async void LogRedirectToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!await LoginCenterAPIWarpper.DoAuth(PluginMain))
                {
                    LogRedirectToggleButton.IsChecked = false;
                }
            }
            catch (Exception)
            {
                LogRedirectToggleButton.IsChecked = false;
            }
        }


 
    }
}
