using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DGJv3
{
    internal class Player : INotifyPropertyChanged
    {
        private Random random = new Random();

        private ObservableCollection<SongItem> Songs;

        private ObservableCollection<SongInfo> Playlist;

        private Dispatcher dispatcher;

        private DispatcherTimer newSongTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromSeconds(1),
            IsEnabled = true,
        };

        private DispatcherTimer updateTimeTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(100),
            IsEnabled = true,
        };

        public UniversalCommand PlayPauseCommand { get; private set; }
        public UniversalCommand NextCommand { get; private set; }

        /// <summary>
        /// 播放器类型
        /// </summary>
        public PlayerType PlayerType { get => _playerType; set => SetField(ref _playerType, value); }
        private PlayerType _playerType;

        /// <summary>
        /// DirectSound 设备
        /// </summary>
        public Guid DirectSoundDevice { get => _directSoundDevice; set => SetField(ref _directSoundDevice, value); }
        private Guid _directSoundDevice;

        /// <summary>
        /// WaveoutEvent 设备
        /// </summary>
        public int WaveoutEventDevice { get => _waveoutEventDevice; set => SetField(ref _waveoutEventDevice, value); }
        private int _waveoutEventDevice;

        /// <summary>
        /// 用户点歌优先
        /// </summary>
        public bool IsUserPrior { get => _isUserPrior; set => SetField(ref _isUserPrior, value); }
        private bool _isUserPrior = false;

        /// <summary>
        /// 单曲最大播放时长
        /// </summary>
        public double MaxPlayTime { get => _maxPlayTime < 60 ? 60 : _maxPlayTime; set => SetField(ref _maxPlayTime, value); }
        private double _maxPlayTime;

        /// <summary>
        /// 当前播放时间
        /// </summary>
        public TimeSpan CurrentTime
        {
            get => mediaFileReader == null ? TimeSpan.Zero : mediaFileReader.CurrentTime;
            set
            {
                if (mediaFileReader != null)
                {
                    mediaFileReader.CurrentTime = value;
                }
            }
        }

        public string CurrentTimeString { get => Math.Floor(CurrentTime.TotalMinutes) + ":" + CurrentTime.Seconds; }

        /// <summary>
        /// 当前播放时间秒数
        /// </summary>
        public double CurrentTimeDouble
        {
            get => CurrentTime.TotalSeconds;
            set => CurrentTime = TimeSpan.FromSeconds(value);
        }

        /// <summary>
        /// 歌曲全长
        /// </summary>
        public TimeSpan TotalTime { get => mediaFileReader == null ? TimeSpan.Zero : mediaFileReader.TotalTime; }

        public string TotalTimeString { get => Math.Floor(TotalTime.TotalMinutes) + ":" + TotalTime.Seconds; }

        /// <summary>
        /// 当前是否正在播放歌曲
        /// </summary>
        public bool IsPlaying
        {
            get => Status == PlayerStatus.Playing;
            set
            {
                if (value)
                {
                    Play();
                }
                else
                {
                    Pause();
                }
            }
        }

        /// <summary>
        /// 当前歌曲播放状态
        /// </summary>
        public PlayerStatus Status
        {
            get
            {
                if (wavePlayer != null)
                {
                    switch (wavePlayer.PlaybackState)
                    {
                        case PlaybackState.Stopped:
                            return PlayerStatus.Stopped;
                        case PlaybackState.Playing:
                            return PlayerStatus.Playing;
                        case PlaybackState.Paused:
                            return PlayerStatus.Paused;
                        default:
                            return PlayerStatus.Stopped;
                    }
                }
                else
                {
                    return PlayerStatus.Stopped;
                }
            }
        }

        /// <summary>
        /// 播放器音量
        /// </summary>
        public float Volume
        {
            get => _volume;
            set
            {
                if (sampleChannel != null)
                {
                    sampleChannel.Volume = value;
                }
                SetField(ref _volume, value, nameof(Volume));
            }
        }
        private float _volume = 1f;

        /// <summary>
        /// 当前歌词
        /// </summary>
        public string CurrentLyric { get => currentLyric; set => SetField(ref currentLyric, value); }
        private string currentLyric;

        /// <summary>
        /// 下一句歌词       
        /// </summary>
        public string UpcomingLyric { get => upcomingLyric; set => SetField(ref upcomingLyric, value); }

        /// <summary>
        /// 是否使用空闲歌单
        /// </summary>
        public bool IsPlaylistEnabled { get => _isPlaylistEnabled; set => SetField(ref _isPlaylistEnabled, value); }
        private bool _isPlaylistEnabled;

        private string upcomingLyric;

        private IWavePlayer wavePlayer = null;

        private MediaFoundationReader mediaFileReader = null;

        private SampleChannel sampleChannel = null;

        public SongItem CurrentSong { get; private set; } = null;

        private int currentLyricIndex = -1;

        public SongInfo LastSongInfo { get; private set; } = null;

        public DanmuHandler DanmuHandler { get; set; }

        public Player(ObservableCollection<SongItem> songs, ObservableCollection<SongInfo> playlist)
        {
            Songs = songs;
            Playlist = playlist;
            dispatcher = Dispatcher.CurrentDispatcher;
            newSongTimer.Tick += NewSongTimer_Tick;
            updateTimeTimer.Tick += UpdateTimeTimer_Tick;
            PropertyChanged += This_PropertyChanged;
            PlayPauseCommand = new UniversalCommand((obj) => { IsPlaying ^= true; });
            NextCommand = new UniversalCommand((obj) => { Next(); });
        }

        private void This_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Status))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
            }
            else if (e.PropertyName == nameof(CurrentTime))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTimeDouble)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTimeString)));
            }
            else if (e.PropertyName == nameof(TotalTime))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalTimeString)));
            }
        }

        /// <summary>
        /// 定时器 100ms 调用一次
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateTimeTimer_Tick(object sender, EventArgs e)
        {
            if (mediaFileReader != null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));

                if (CurrentSong != null)
                {
                    var index = CurrentSong.Lyric.GetLyric(CurrentTimeDouble, out string current, out string upcoming);
                    if (index != currentLyricIndex)
                    {
                        currentLyricIndex = index;
                        SetLyric(current, upcoming);
                    }
                }
            }
        }

        /// <summary>
        /// 定时器 1s 调用一次
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewSongTimer_Tick(object sender, EventArgs e)
        {
            if (Songs.Count > 0 && Songs[0].Status == SongStatus.WaitingPlay)
            {
                LoadSong(Songs[0]);
            }

            FillSongsWithSparePlaylist();

            if (MaxPlayTime <= CurrentTimeDouble)
            {
                Next();
            }
        }

        private void FillSongsWithSparePlaylist()
        {
            if (Songs.Count < 2 && IsPlaylistEnabled && Playlist.Count > 0)
            {
                int index = -1;
                int time = 0;
                do
                {
                    index = random.Next(0, Playlist.Count);
                    time++;
                } while (Songs.Any(ele => Playlist[index].Id == ele.SongId) && time < 3);


                SongInfo info = Playlist[index];
                if (info.Lyric == null)
                {
                    info.Lyric = info.Module.SafeGetLyric(info);
                }
                SongItem item = new SongItem(info, Utilities.SparePlaylistUser);
                Songs.Add(item);
            }
        }



        /// <summary>
        /// 加载歌曲并开始播放
        /// </summary>
        /// <param name="songItem"></param>
        private void LoadSong(SongItem songItem)
        {
            CurrentSong = songItem;

            CurrentSong.Status = SongStatus.Playing;

            wavePlayer = CreateIWavePlayer();
            try
            {
                mediaFileReader = new MediaFoundationReader(CurrentSong.FilePath);
                sampleChannel = new SampleChannel(mediaFileReader)
                {
                    Volume = Volume
                };

                wavePlayer.PlaybackStopped += (sender, e) => UnloadSong();

                wavePlayer.Init(sampleChannel);
                wavePlayer.Play();

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalTime)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));

            }
            catch (Exception ex)
            {
                Log($"下载出错，可能为VIP/无版权/网络波动。“{songItem.SongName}”（{songItem.ModuleName}）", ex);
                UnloadSong();
            }
        }

        /// <summary>
        /// 卸载歌曲并善后
        /// </summary>
        private void UnloadSong()
        {
            try
            {
                wavePlayer?.Dispose();
            }
            catch (Exception) { }

            try
            {
                mediaFileReader?.Dispose();
            }
            catch (Exception) { }

            wavePlayer = null;
            sampleChannel = null;
            mediaFileReader = null;

            try
            {
                File.Delete(CurrentSong.FilePath);
            }
            catch (Exception)
            {
            }

            dispatcher.Invoke(() => Songs.Remove(CurrentSong));

            LastSongInfo = CurrentSong.Info;

            CurrentSong = null;

            SetLyric(string.Empty, string.Empty);

            // TODO: PlayerBroadcasterLoop 功能

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalTime)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));

            DanmuHandler.AfterUnloadSong();
        }

        private void SetLyric(string current, string upcoming)
        {
            CurrentLyric = current;
            UpcomingLyric = upcoming;
            Task.Run(() => LyricEvent?.Invoke(this, new LyricChangedEventArgs()
            {
                CurrentLyric = current,
                UpcomingLyric = upcoming
            }));
        }

        /// <summary>
        /// 根据当前设置初始化 IWavePlayer
        /// </summary>
        /// <returns></returns>
        private IWavePlayer CreateIWavePlayer()
        {
            switch (PlayerType)
            {
                case PlayerType.WaveOutEvent:
                    return new WaveOutEvent() { DeviceNumber = WaveoutEventDevice };
                case PlayerType.DirectSound:
                    return new DirectSoundOut(DirectSoundDevice);
                default:
                    return null;
            }
        }

        /// <summary>
        /// 对外接口 继续
        /// <para>
        /// 注：此接口可在任意线程同步调用
        /// </para>
        /// </summary>
        public void Play()
        {
            if (wavePlayer != null)
            {
                wavePlayer.Play();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }

        }

        /// <summary>
        /// 对外接口 暂停
        /// <para>
        /// 注：此接口可在任意线程同步调用
        /// </para>
        /// </summary>
        public void Pause()
        {
            if (wavePlayer != null)
            {
                wavePlayer.Pause();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }

        }

        /// <summary>
        /// 对外接口 下一首
        /// <para>
        /// 注：此接口可在任意线程同步调用
        /// </para>
        /// </summary>
        public void Next()
        {
            if (wavePlayer != null)
            {
                wavePlayer.Stop();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public event LyricChangedEvent LyricEvent;

        public event LogEvent LogEvent;
        private void Log(string message, Exception exception) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });
    }
}
