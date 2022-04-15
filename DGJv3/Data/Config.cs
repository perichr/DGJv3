using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        internal static Config Load()
        {
            Config config = null;
            try
            {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Utilities.ConfigFilePath, Encoding.UTF8));
            }
            catch
            {
            }
            if (config?.Playlist == null)
            {
                config = new Config();
            }
            return config;
        }

        internal static void Write(Config config)
        {
            if (config?.UsingModules != null)
            {
                try
                {
                    File.WriteAllText(Utilities.ConfigFilePath, JsonConvert.SerializeObject(config), Encoding.UTF8);
                }
                catch
                {
                }

            }
        }
    }
}
