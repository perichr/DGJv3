using BilibiliDM_PluginFramework;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DGJv3
{
    public class SongItem : INotifyPropertyChanged
    {

        internal SongItem(SongInfo songInfo, string userName)
        {
            Status = SongStatus.WaitingDownload;
            UserName = userName;
            Info = songInfo;

            Lyric = (songInfo.Lyric == null) ? Lrc.NoLyric : Lrc.InitLrc(songInfo.Lyric);

        }
        /// <summary>
        /// 歌曲信息
        /// </summary>
        public SongInfo Info
        { get; internal set; }

        /// <summary>
        /// 搜索模块名称
        /// </summary>
        public string ModuleName
        { get => Info.Module.ModuleName;  }


        /// <summary>
        /// 搜索模块
        /// </summary>
        internal SearchModule Module
        { get => Info.Module; }

        /// <summary>
        /// 歌曲ID
        /// </summary>
        public string SongId
        { get=>Info.Id; }

        /// <summary>
        /// 歌名
        /// </summary>
        public string SongName
        { get=>Info.Name; }

        /// <summary>
        /// string的歌手列表
        /// </summary>
        public string SingersText
        { get => Info.SingersText; }
        

        /// <summary>
        /// 歌手列表
        /// </summary>
        public string[] Singers
        { get=>Info.Singers; }

        /// <summary>
        /// 点歌人
        /// </summary>
        public string UserName
        { get; internal set; }

        /// <summary>
        /// 歌曲文件储存路径
        /// </summary>
        public string FilePath
        { get; internal set; }

        /// <summary>
        /// 文本歌词
        /// </summary>
        public Lrc Lyric
        { get; internal set; }

        /// <summary>
        /// 歌曲备注
        /// </summary>
        public string Note
        { get => Info.Note; }

        /// <summary>
        /// 歌曲备注
        /// </summary>
        public IDictionary<string,string> ExtInfo
        { get=>Info.ExtInfo; }

        public string GetInfo(string key)
        {
            return Info.GetInfo(key);
        }

        public bool IsAddedByUser(string userName)
        {
            return UserName == userName;
        }
        public bool IsAddedByUser(DanmakuModel danmakuModel)
        {
            return IsAddedByUser(danmakuModel.UserName);
        }
        public bool IsPlaying
        {
            get => Status == SongStatus.Playing;
        }

        /// <summary>
        /// 歌曲状态
        /// </summary>
        public SongStatus Status
        { get => _status; internal set => SetField(ref _status, value); }

        private SongStatus _status;

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}