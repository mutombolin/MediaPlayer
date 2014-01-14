using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using MediaPlayer.Settings;
using MediaPlayer.Managers;

namespace MediaPlayer.Windows
{
    /// <summary>
    /// Interaction logic for WinMediaPlayer.xaml
    /// </summary>
    public partial class WinMediaPlayer : Window
    {
        private double _volume;
        private const double NominalVideoSpeed = 1.0;
        private MediaPlayState _mediaPlayState;
        private MediaFileType _mediaFileType;
        private string _fileName;
        private bool _isMuted;
        private bool _isFastForward;
        private int _timeShift;
        private int _stepCount;

        private System.Timers.Timer _endTimer;

        public static readonly WinMediaPlayer Instance = new WinMediaPlayer();

        public event EventHandler OnMediaEnded;

        private PortableDevice.MediaServer _mediaServer;

        private WinMediaPlayer()
        {
            InitializeComponent();

            _mediaPlayState = MediaPlayState.Stop;
            _mediaFileType = MediaFileType.Audio;
            _fileName = string.Empty;
            _isMuted = false;
            _isFastForward = false;
            _timeShift = 1;
            _stepCount = 0;
            _volume = 1.0;

            _mediaServer = new PortableDevice.MediaServer();
            _mediaServer.Start();

//            PicardLib.Settings.PersistentSettings.SettingChangeHandler += new PicardLib.Settings.PersistentSettings.SettingChange(PersistentSettings_SettingChangeHandler);

            mediaElementMainVideo.MediaEnded += new RoutedEventHandler(mediaElementMainVideo_MediaEnded);
            mediaElementMainVideo.MediaFailed += new EventHandler<ExceptionRoutedEventArgs>(mediaElementMainVideo_MediaFailed);

//            CalculateVolume(PicardLib.Settings.PersistentSettings.MediaPlayerVolumeDefault);

            _endTimer = new System.Timers.Timer();
            _endTimer.Interval = 300;
            _endTimer.AutoReset = true;
            _endTimer.Elapsed += new System.Timers.ElapsedEventHandler(_endTimer_Elapsed);
        }

        public void Dispose()
        {
            mediaElementMainVideo.Stop();
            _mediaServer.Stop();
            _mediaServer.Dispose();
        }

        void mediaElementMainVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (e.ErrorException != null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    e.ErrorException,
                    "winmediaplayer.mediaElementMainVideo_MediaFailed: exception"));
            }

            //_mediaPlayState = MediaPlayState.Stop;
        }

        void mediaElementMainVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            _mediaPlayState = MediaPlayState.Stop;

            if (OnMediaEnded != null)
                OnMediaEnded(this, new EventArgs());
        }

        private void CalculateVolume(int volume)
        {
//            double volumeStep = 1.0 / AudioManager.VolumeQuickSteps;
//            _volume = volumeStep * volume;
        }

        public string FileName
        {
            set
            {
                _fileName = value;
                if (!string.IsNullOrEmpty(_fileName))
                {
                    PortableDevice.PortableDeviceObject item = null;

                    foreach (PortableDevice.PortableDeviceObject obj in MediaContentManager.Instance.MusicPlayList)
                    {
                        if (string.Compare(_fileName, obj.Name, true) == 0)
                        {
                            item = obj;
                            break;
                        }
                    }
//                    PortableDevice.PortableDeviceFile item = (PortableDevice.PortableDeviceFile)MediaContentManager.Instance.MusicPlayDictionary[_fileName];
                    if (item != null)
                    {
                        _mediaServer.Device = MediaContentManager.Instance.Device;
                        _mediaServer.FileObject = (PortableDevice.PortableDeviceFile)item;
                    }
                }
            }
            get
            {
                return _fileName;
            }
        }
/*
        public string FileName
        {
            set
            {
                try
                {
                    _fileName = value;
                    if (_fileName != string.Empty)
                        mediaElementMainVideo.Source = new Uri(value, UriKind.RelativeOrAbsolute);
                    else
                        mediaElementMainVideo.Source = null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                        System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                        ex,
                        "winmediaplayer.videofilename: exception"));
                }
            }
            get
            {
                return _fileName;
            }
        }
*/
        public int PercentProgress
        {
            get
            {
                int result = 0;

                try
                {
                    if (!mediaElementMainVideo.NaturalDuration.HasTimeSpan || (mediaElementMainVideo.NaturalDuration.TimeSpan.TotalSeconds == 0))
                        return result;

                    result = (int)(mediaElementMainVideo.Position.TotalSeconds / mediaElementMainVideo.NaturalDuration.TimeSpan.TotalSeconds * 100.0);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                        System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                        ex,
                        "Failed to set UI state from media element status!"));
                }

                return result;
            }
        }

        public double MediaPlayerVolume
        {
            get { return _volume; }
        }

        public void Play()
        {
            try
            {
#if USE_SOFTWARE_DECODING
                System.Windows.Interop.HwndSource hwndSource = PresentationSource.FromVisual(this) as System.Windows.Interop.HwndSource;
                System.Windows.Interop.HwndTarget hwndTarget = hwndSource.CompositionTarget;
                hwndTarget.RenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                textBlockError.Text = "software decoding on";
#endif
                mediaElementMainVideo.Source = new Uri(@"http://localhost:7896/", UriKind.Absolute);
                mediaElementMainVideo.Play();
                mediaElementMainVideo.IsMuted = _isMuted;
                mediaElementMainVideo.Volume = _volume;
                _mediaPlayState = MediaPlayState.Play;
                mediaElementMainVideo.SpeedRatio = NominalVideoSpeed;
                _isFastForward = true;
                _timeShift = 1;
                _stepCount = 0;
            }
            catch (Exception ex)
            {
                _mediaPlayState = MediaPlayState.Stop;

                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "winmediaplayer.play: exception"));
            }
        }

        void _endTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Timers.ElapsedEventHandler(SAFE_endTimer_Elapsed), sender, e);
        }

        void SAFE_endTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            int shift = 0;

            if (_stepCount >= 3)
            {
                _timeShift <<= 1;
                _stepCount = 0;
            }

            if (_timeShift >= 32)
                _timeShift = 32;

            if (_isFastForward)
                shift = _timeShift * 1;
            else
                shift = _timeShift * -1;

            mediaElementMainVideo.Position = mediaElementMainVideo.Position.Add(new TimeSpan(0, 0, shift));
            _stepCount++;
        }

        public void FastForward()
        {
            if (_mediaPlayState != MediaPlayState.Play)
                return;

            _isFastForward = true;

            Pause();
            _endTimer.Start();
        }

        public void EndFastForward()
        {
            if (!_endTimer.Enabled)
                return;

            _endTimer.Stop();
            Play();
        }

        public void Rewind()
        {
            if (_mediaPlayState != MediaPlayState.Play)
                return;

            _isFastForward = false;

            Pause();
            _endTimer.Start();
        }

        public void EndRewind()
        {
            if (!_endTimer.Enabled)
                return;

            _endTimer.Stop();
            Play();
        }

        public void Pause()
        {
            try
            {
                mediaElementMainVideo.Pause();
                _mediaPlayState = MediaPlayState.Pause;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "winmediaplayer.pause: exception"));
            }
        }

        public void Stop()
        {
            try
            {
                mediaElementMainVideo.Stop();
                mediaElementMainVideo.Source = null;
                _mediaPlayState = MediaPlayState.Stop;
                _isFastForward = true;
                _timeShift = 1;
                _stepCount = 0;
                _mediaServer.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "winmediaplayer.stop: exception"));
            }
        }

        public void Mute(bool mute)
        {
            try
            {
                mediaElementMainVideo.IsMuted = mute;
                _isMuted = mute;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "winmediaplayer.ismuted: exception"));
            }
        }

        public MediaPlayState MediaPlayState
        {
            get { return _mediaPlayState; }
        }

        public MediaFileType MediaFileType
        {
            get { return _mediaFileType; }
            set { _mediaFileType = value; }
        }

        public bool IsMuted
        {
            get { return _isMuted; }
        }
    }
}
