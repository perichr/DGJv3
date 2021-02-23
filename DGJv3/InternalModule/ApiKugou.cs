using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DGJv3.InternalModule
{
    sealed class ApiKugou : ApiBaseModule
    {
        private const string API_PROTOCOL = "https://";
        private const string API_HOST = "www.kugou.com";
        private const string API_PATH = "/yy";
        private const string COOKIES = "kg_mid=355721c2749fe30472161adf09b5748d";
        internal ApiKugou()
        {
            SetServiceName("kugou");
            SetInfo("酷狗音乐", INFO_AUTHOR, INFO_EMAIL, INFO_VERSION, "搜索酷狗音乐的歌曲");
        }
        public string RandomIntString()
        {
            Random random = new Random();
            int num = random.Next(1, 99);
            return Convert.ToString(num);
        }

        public string TimestampString()
        {
            DateTime dateTime = DateTime.Now;
            return dateTime.ToUniversalTime().ToString("O");
        }

        protected override string GetDownloadUrl(SongItem songItem)
        {
            try
            {
                string jqueryHeader = $"jQuery1910{RandomIntString()}_{TimestampString()}";
                string resualt = Fetch(
                    API_PROTOCOL,
                    "wwwapi.kugou.com",
                    API_PATH +  $"/index.php?r=play/getdata&hash={songItem.SongId}&album_id={songItem.GetInfo("albumid")}",
                    null,
                    "https://www.kugou.com/",
                    COOKIES);
                JObject dlurlobj = JObject.Parse(resualt);
                string url = dlurlobj.SelectToken("data.play_url")?.ToString();
                return url;
            }
            catch (Exception ex)
            {
                Log($"歌曲 {songItem.SongName} 不能下载(ex:{ex.Message})");
                return null;
            }
        }
        protected override string GetLyric(SongItem songItem)
        {
            return GetLyricBySongInfo(songItem.Info);
        }
        string GetLyricBySongInfo(SongInfo songInfo)
        {
            try
            {
                var response = Fetch(API_PROTOCOL,
                    API_HOST,
                    API_PATH + $"/index.php?r=play/getdata&hash={songInfo.Id}&album_id={songInfo.GetInfo("albumid")}",
                    null,
                    "https://www.kugou.com/",
                    COOKIES);
                var json = JObject.Parse(response);
                var lyric = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(json.SelectToken("data.lyrics")?.ToString()));
                return lyric;
            }
            catch (Exception ex)
            {
                Log($"歌曲 {songInfo.Id} 歌词下载错误(ex:{ex.Message})");
                return null;
            }
        }


        protected override List<SongInfo> GetPlaylist(string id)
        {
            try
            {
                var response = Fetch("http://", "m.kugou.com",
                    $"/plist/list/{id}?json=true",
                    null,
                    $"http://m.kugou.com/plist/list/{id}",
                    COOKIES);

                var json = JObject.Parse(response);
                return (json.SelectToken("list.list.info") as JArray)?.Select(song =>
                {
                    SongInfo songInfo;
                    try
                    {
                        string[] filename = song["filename"].ToString().Split(new char[3] { ' ', '-', ' ' });
                        songInfo = new SongInfo(
                            this,
                            song["hash"].ToString(),
                            filename[0],
                            filename[1].Split('\u3001')
                        );
                        songInfo.SetInfo("albumid", song["album_id"].ToString());
                    }
                    catch (Exception ex)
                    { Log("歌曲信息获取结果错误：" + ex.Message); return null; }

                    return songInfo;
                }).ToList();
            }
            catch (Exception ex)
            {
                Log($"歌单下载错误(ex:{ex.Message})");
                return null;
            }
        }

        protected override SongInfo Search(string keyword)
        {
            try
            {
                var response = Fetch(API_PROTOCOL, "songsearch.kugou.com",
                    $"/song_search_v2?keyword={keyword}&pagesize=1");
                var json = JObject.Parse(response);
                var song = json.SelectToken("data.lists[0]");
                SongInfo songInfo;
                songInfo = new SongInfo(
                    this,
                    song["FileHash"].ToString(),
                    song["SongName"].ToString(),
                    song["SingerName"].ToString().Split('\u3001')
                );
                songInfo.SetInfo("albumid", song["AlbumID"].ToString());
                songInfo.Lyric = GetLyricBySongInfo(songInfo);

                return songInfo;
            }
            catch (Exception ex)
            { Log("歌曲信息获取结果错误：" + ex.Message); return null; }
        }
    }
}
