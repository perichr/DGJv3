using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace DGJv3
{
    class Config
    {
        [JsonProperty("ptyp")]
        public PlayerType PlayerType { get; set; } = PlayerType.DirectSound;

        [JsonProperty("pdsd")]
        public Guid DirectSoundDevice { get; set; } = Guid.Empty;

        [JsonProperty("pwed")]
        public int WaveoutEventDevice { get; set; } = -1;

        [JsonProperty("pvol")]
        public float Volume { get; set; } = 0.5f;

        [JsonProperty("pple")]
        public bool IsPlaylistEnabled { get; set; } = true;

        [JsonProperty("dmpt")]
        public double MaxPlayTime { get; set; } = 600;

        [JsonProperty("acps")]
        public bool IsAllowCancelPlayingSong { get; set; } = true;

        [JsonProperty("dmts")]
        public uint MaxTotalSongNum { get; set; } = 10;

        [JsonProperty("dmps")]
        public uint MaxPersonSongNum { get; set; } = 2;

        [JsonProperty("up")]
        public bool IsUserPrior { get; set; } = true;

        [JsonProperty("lrd")]
        public bool IsLogRedirectDanmaku { get; set; } = false;

        [JsonProperty("ldll")]
        public int LogDanmakuLengthLimit { get; set; } = 20;

        [JsonProperty("blst")]
        public BlackListItem[] Blacklist { get; set; } = new BlackListItem[0];

        [JsonProperty("acmd")]
        public string AdminCommand { get; set; } = "切歌 暂停 播放 音量";

        [JsonProperty("vnext")]
        public int Vote4NextCount { get; set; } = 2;

        [JsonProperty("mlst")]
        public string[] UsingModules { get; set; } = new string[0];

        [JsonProperty("alst")]
        public string AdminList { get; set; } = "";

        [JsonProperty("sbtp")]
        public string ScribanTemplate { get; set; } = "{{~ for 歌曲 in 播放列表 ~}}{{ if for.index ==1\n" +
            "break\n" +
            "end}}正在播放【 {{当前播放时间}} / {{当前总时间}} 】\n" +
            "【{{  歌曲.点歌人 }}】{{ 歌曲.歌名 }} - {{ 歌曲.歌手 }}\n" +
            "{{~  end ~}}\n" +
            "\n" +
            "等待播放【 {{ 歌曲数量 - 1 }} / {{ 总共最大点歌数量 -1 }} 】\n" +
            "{{~ for 歌曲 in 播放列表 ~}}\n" +
            "{{~if for.index == 0\n" +
            "continue\n" +
            "end~}}\n" +
            "【{{  歌曲.点歌人 }}】{{ 歌曲.歌名 }} -  {{ 歌曲.歌手 }} \n" +
            "{{~  end ~}}";

        [JsonProperty("plst")]
        public SongInfo[] Playlist { get; set; } = new SongInfo[0];


        public Config()
        {
        }

        internal static Config Load(string path = null)
        {
            if (string.IsNullOrEmpty(path))
                path = Utilities.ConfigFilePath;
            Config config ;
            Config empty = new Config();
            try
            {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path, Encoding.UTF8));
                PropertyInfo[] pis = typeof(Config).GetProperties(BindingFlags.Public);
                foreach (PropertyInfo pi in pis)
                {
                    if (pi.GetValue(config) == null)
                        pi.SetValue(config, pi.GetValue(empty));
                }
            }
            catch
            {
                config = empty;
            }

            return config;
        }

        internal static void Write(Config config, bool backup = false)
        {
            if (config?.UsingModules != null)
            {
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
