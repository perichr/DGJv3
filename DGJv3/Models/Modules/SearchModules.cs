using DGJv3.InternalModule;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text.RegularExpressions;

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

            AddModule(new ApiNetease());
            AddModule(new ApiTencent());
            AddModule(new ApiKugou());
            AddModule(new ApiKuwo());
            AddModule(new ApiBiliBiliMusic());

            UsingModules = new ObservableCollection<SearchModule>();

        }

        public void MoveUsingModule(SearchModule searchModule, bool up)
        {
            if (searchModule == null)
            {
                return;
            }
            int index1 = UsingModules.IndexOf(searchModule);
            int index2 = up ? index1 - 1 : index1 + 1;
            try
            {
                var temp = UsingModules[index1];
                UsingModules[index1] = UsingModules[index2];
                UsingModules[index2] = temp;
            }
            catch { }

        }

        public SongInfo GetSongInfo(string keyword)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string[] tmp = keyword.Split(Utilities.JOIN_STRING.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length > 1)
                {
                    if (Regex.IsMatch(tmp[0], @"\d"))
                    {
                        int m = int.Parse(tmp[0]);
                        if (m < UsingModules.Count)
                        {
                            return UsingModules[m].SafeSearch(string.Join(Utilities.JOIN_STRING, tmp.Skip(1)));
                        }
                    }
                }
                foreach (SearchModule searchModule in UsingModules)
                {
                    SongInfo songInfo = searchModule.SafeSearch(keyword);
                    if (songInfo != null) return songInfo;
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
