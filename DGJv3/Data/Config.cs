using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace DGJv3
{
    class Config : INotifyPropertyChanged
    {
        /// <summary>
        /// 播放器类型
        /// </summary>
        [JsonProperty("ptyp")]
        public PlayerType PlayerType { get => _playerType; set => SetField(ref _playerType, value); }
        private PlayerType _playerType = PlayerType.DirectSound;

        /// <summary>
        /// DirectSound 设备
        /// </summary>
        [JsonProperty("pdsd")]
        public Guid DirectSoundDevice { get => _directSoundDevice; set => SetField(ref _directSoundDevice, value); }
        private Guid _directSoundDevice = Guid.Empty;

        /// <summary>
        /// WaveoutEvent 设备
        /// </summary>
        [JsonProperty("pwed")]
        public int WaveoutEventDevice { get => _waveoutEventDevice; set => SetField(ref _waveoutEventDevice, value); } 
        private int _waveoutEventDevice= -1;

        /// <summary>
        /// 播放器音量
        /// </summary>
        [JsonProperty("pvol")]
        public float Volume { get; set; } = 1f;

        /// <summary>
        /// 是否使用空闲歌单
        /// </summary>
        [JsonProperty("pple")]
        public bool IsPlaylistEnabled { get => _isPlaylistEnabled; set => SetField(ref _isPlaylistEnabled, value); } 
        private bool _isPlaylistEnabled= true;

        /// <summary>
        /// 单曲最大播放时长
        /// </summary>
        [JsonProperty("dmpt")]
        public double MaxPlayTime { get => CheckMaxPlayTime(_maxPlayTime); set => SetField(ref _maxPlayTime, value); } 
        private double _maxPlayTime= 600;
        private double CheckMaxPlayTime(double value)
        {
            return value < 60 ? 60 : value;
        }

        /// <summary>
        /// 用户点歌优先
        /// </summary>
        [JsonProperty("up")]
        public bool IsUserPrior { get => _isUserPrior; set => SetField(ref _isUserPrior, value); } 
        private bool _isUserPrior = true;

        /// <summary>
        /// 允许取消正在播放的歌曲
        /// </summary>
        [JsonProperty("acps")]
        public bool IsAllowCancelPlayingSong { get => _isAllowCancelPlayingSong; set => SetField(ref _isAllowCancelPlayingSong, value); } 
        private bool _isAllowCancelPlayingSong= true;

        /// <summary>
        /// 最多点歌数量
        /// </summary>
        [JsonProperty("dmts")]
        public uint MaxTotalSongNum { get => _maxTotalSongCount; set => SetField(ref _maxTotalSongCount, value); } 
        private uint _maxTotalSongCount= 13;

        /// <summary>
        /// 每个人最多点歌数量
        /// </summary>
        [JsonProperty("dmps")]
        public uint MaxPersonSongNum { get => _maxPersonSongNum; set => SetField(ref _maxPersonSongNum, value); }
        private uint _maxPersonSongNum = 2;

        /// <summary>
        /// 是否反馈弹幕
        /// </summary>
        [JsonProperty("lrd")]
        public bool IsLogRedirectDanmaku { get => _isLogRedirectDanmaku; set => SetField(ref _isLogRedirectDanmaku, value); }
        private bool _isLogRedirectDanmaku = false;

        /// <summary>
        /// 反馈弹幕允许长度
        /// </summary>
        [JsonProperty("ldll")]
        public int LogDanmakuLengthLimit { get => _logDanmakuLengthLimit; set => SetField(ref _logDanmakuLengthLimit, value); }
        private int _logDanmakuLengthLimit = 20;

        /// <summary>
        /// 下一首需要投票人数
        /// </summary>
        [JsonProperty("vnext")]
        public int Vote4NextCount { get => _vote4NextCount; set => SetField(ref _vote4NextCount, value); } 
        private int _vote4NextCount = 2;    

        /// <summary>
        /// 房管权限指令
        /// </summary>
        [JsonProperty("acmd")]
        public string AdminCommand
        {
            get => _adminCommand;
            set
            {
                if (value == null) value = "";
                else
                {
                    Regex regClean = new Regex(@"[\s,，;；]{1,}", RegexOptions.IgnoreCase);
                    value = regClean.Replace(value, Utilities.JOIN_STRING).Trim();
                }
                SetField(ref _adminCommand, value);
            }
        }
        private string _adminCommand = "切歌 暂停 播放 音量";



        /// <summary>
        /// 手动房管清单
        /// </summary>
        [JsonProperty("alst")]
        public string AdminList
        {
            get => _adminList;
            set
            {
                value = String.Join(Environment.NewLine, value.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
                SetField(ref _adminList, value);
            }
        }
        private string _adminList = "";


        /// <summary>
        /// 曲池信息模板
        /// </summary>
        [JsonProperty("sbtp")]
        public string ScribanTemplate { get; set; } = "{{~ for 歌曲 in 播放列表 ~}}{{ if for.index ==1\n" +
            "break\n" +
            "end}}正在播放【 {{当前播放时间}} / {{当前总时间}} 】\n" +
            "【{{  歌曲.点歌人 }}】{{ 歌曲.歌名 }} - {{ 歌曲.歌手 }}\n" +
            "{{  歌曲.搜索模块 }}：{{ 歌曲.歌曲id }}\n" +
            "{{~  end ~}}\n" +
            "\n" +
            "等待播放【 {{ 歌曲数量 - 1 }} / {{ 总共最大点歌数量 -1 }} 】\n" +
            "{{~ for 歌曲 in 播放列表 ~}}\n" +
            "{{~if for.index == 0\n" +
            "continue\n" +
            "end~}}\n" +
            "【{{  歌曲.点歌人 }}】{{ 歌曲.歌名 }} -  {{ 歌曲.歌手 }} \n" +
            "{{~  end ~}}";

        /// <summary>
        /// 使用的模块
        /// </summary>
        [JsonProperty("mlst")]
        public string[] UsingModules { get; set; } = new string[0];

        /// <summary>
        /// 黑名单
        /// </summary>
        [JsonProperty("blst")]
        public BlackListItem[] Blacklist { get; set; } = new BlackListItem[0];

        /// <summary>
        /// 空闲歌单
        /// </summary>
        [JsonProperty("plst")]
        public SongInfo[] Playlist { get; set; } = new SongInfo[0];









        public Config()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }


        public static Config Current { get; private set; }


        internal static Config Load(string path = null)
        {
            if (string.IsNullOrEmpty(path))
                path = Utilities.ConfigFilePath;
            Config init = new Config();
            try
            {
                Current = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path, Encoding.UTF8));
                PropertyInfo[] pis = typeof(Config).GetProperties(BindingFlags.Public);
                foreach (PropertyInfo pi in pis)
                {
                    if (pi.GetValue(Current) == null)
                        pi.SetValue(Current, pi.GetValue(init));
                }
            }
            catch
            {
                Current = init;
                Write(Current);
            }
            return Current;
        }

        internal static void Write(Config config, bool backup = false)
        {
            if (config == null)
            {
                config = Current;
            }
            try
            {
                File.WriteAllText(Utilities.ConfigFilePath, JsonConvert.SerializeObject(config), Encoding.UTF8);
                if (backup)
                    File.Copy(Utilities.ConfigFilePath, Config.GetConfigPath(File.GetLastWriteTime(Utilities.ConfigFilePath)), true);
            }
            catch
            {
            }

        }

        internal static void Write(bool backup = false)
        {
            try
            {
                File.WriteAllText(Utilities.ConfigFilePath, JsonConvert.SerializeObject(Current), Encoding.UTF8);
                if (backup)
                    File.Copy(Utilities.ConfigFilePath, Config.GetConfigPath(File.GetLastWriteTime(Utilities.ConfigFilePath)), true);
            }
            catch { }
        }

        public static string GetConfigPath(string key)
        {
            return Path.Combine(Utilities.ConfigBackupDirectoryPath, "config." + key + ".json");
        }

        public static string GetConfigPath(DateTime dt)
        {
            return GetConfigPath(dt.ToString("yyyyMMddHHmmss"));
        }

    }
}
