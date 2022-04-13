using DGJv3.InternalModule;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace DGJv3
{
    class SearchModules : INotifyPropertyChanged
    {
        public SearchModule NullModule { get; private set; }
        public ObservableCollection<SearchModule> Modules { get; set; }
        public ObservableCollection<SearchModule> UsingModules { get; set; }

        internal SearchModules()
        {
            Modules = new ObservableCollection<SearchModule>();

            NullModule = new NullSearchModule();
            AddModule(NullModule);

            AddModule(new ApiNetease());
            AddModule(new ApiTencent());
            AddModule(new ApiKugou());
            AddModule(new ApiKuwo());
            AddModule(new ApiBiliBiliMusic());

            UsingModules = new ObservableCollection<SearchModule>();

        }

        public SongInfo GetSongInfo(string keyword, int loop = 2)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                int i = 0;
                loop = (loop < 1) ? UsingModules.Count : Math.Min(UsingModules.Count, loop);
                SongInfo songInfo;
                while (i < loop)
                {
                    SearchModule searchModule = UsingModules[i];
                    if(searchModule == null || searchModule == NullModule)
                    {
                        continue;
                    }
                    songInfo = searchModule.SafeSearch(keyword);
                    if(songInfo != null)
                    {
                        return songInfo;
                    }
                    i++;
                }             
            }
            return null;
        }

        public List<SongInfo> GetSongInfoList(string keyword)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                SearchModule searchModule = UsingModules.FirstOrDefault(x => x.IsPlaylistSupported);
                if (searchModule != null)
                {
                    return searchModule.SafeGetPlaylist(keyword);
                }
            }
            return null;
        }

        public void AddModule(SearchModule m)
        {
            Modules.Add(m);
            m._log = logaction;
        }
        void logaction(string log)
        {
            Log(log);
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
    }
}
