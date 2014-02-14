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
using System.Windows.Interop;
using System.Windows.Forms;
using System.Threading;

using System.Runtime.InteropServices;

using MediaPlayer.Settings;
using MediaPlayer.Managers;
using MediaPlayer.Data;


using WmpFormsLib;

namespace MediaPlayer.Windows
{
    /// <summary>
    /// Interaction logic for WinMediaPlayer.xaml
    /// </summary>
    public partial class WinMediaPlayer : Window
    {
        private double _volume = 0;
        private double _volumeQuickSteps = 30;
        private const double NominalVideoSpeed = 1.0;
        private MediaPlayState _mediaPlayState;
        private MediaFileType _mediaFileType;
        private DeviceType _mediaType;
        private PortableDevice.PortableDeviceObject _object;
        private string _fileName;
        private bool _isMuted;
        private bool _isFastForward;
        private int _timeShift;
        private int _stepCount;
        private bool _isOpened;
        private uint _totalTime = 0;
        private uint _duration = 0;
        private string _source = string.Empty;

        private IntPtr _hwndParent;

        private System.Timers.Timer _endTimer;

        public static readonly WinMediaPlayer Instance = new WinMediaPlayer();

        public event EventHandler OnMediaEnded;
        public event EventHandler OnFullScreen;

        private PortableDevice.MediaServer _mediaServer;

        private WmpFormsLib.WmpControl _wmpForm = null;
        private AxWMPLib.AxWindowsMediaPlayer mediaElementMainVideo = null;
        private System.Windows.Forms.Label _textBlockError = null;
        private System.Windows.Forms.Label _textBlockMessage = null;

        private System.Timers.Timer _searchResumeTimer;

        [DllImport("User32.dll", EntryPoint = "SetParent")]
        public static extern IntPtr SetParent(IntPtr hWnd, IntPtr hParent);

        private WinMediaPlayer()
        {
            InitializeComponent();

            _mediaPlayState = MediaPlayState.Stop;
            _mediaFileType = MediaFileType.Audio;
            _mediaType = DeviceType.PortableDevice;
            _object = null;
            _fileName = string.Empty;
            _isMuted = false;
            _isFastForward = false;
            _timeShift = 1;
            _stepCount = 0;
            _isOpened = false;

            _mediaServer = new PortableDevice.MediaServer();
            _mediaServer.Start();

            _wmpForm = new WmpFormsLib.WmpControl();
            formsHost.Child = _wmpForm;
            _textBlockError = _wmpForm._textBlockError;
            _textBlockMessage = _wmpForm._textBlockMessage;

            mediaElementMainVideo = _wmpForm._player;
            mediaElementMainVideo.uiMode = "none";
            mediaElementMainVideo.settings.setMode("loop", false);
            mediaElementMainVideo.stretchToFit = true;
            mediaElementMainVideo.enableContextMenu = false;
            mediaElementMainVideo.Ctlenabled = false;
            mediaElementMainVideo.ErrorEvent += new EventHandler(mediaElementMainVideo_ErrorEvent);

//            PicardLib.Settings.PersistentSettings.SettingChangeHandler += new PicardLib.Settings.PersistentSettings.SettingChange(PersistentSettings_SettingChangeHandler);

            mediaElementMainVideo.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(mediaElementMainVideo_PlayStateChange);
            mediaElementMainVideo.MouseDownEvent += new AxWMPLib._WMPOCXEvents_MouseDownEventHandler(mediaElementMainVideo_MouseDownEvent);
//            mediaElementMainVideo.MediaEnded += new RoutedEventHandler(mediaElementMainVideo_MediaEnded);
//            mediaElementMainVideo.MediaFailed += new EventHandler<ExceptionRoutedEventArgs>(mediaElementMainVideo_MediaFailed);
//            mediaElementMainVideo.MediaOpened += new RoutedEventHandler(mediaElementMainVideo_MediaOpened);
//            mediaElementMainVideo.BufferingEnded += new RoutedEventHandler(mediaElementMainVideo_BufferingEnded);
//            mediaElementMainVideo.MouseDown += new MouseButtonEventHandler(mediaElementMainVideo_MouseDown);

            _mediaServer.OnLoadingFinished += new EventHandler(_mediaServer_OnLoadingFinished);

            _endTimer = new System.Timers.Timer();
            _endTimer.Interval = 300;
            _endTimer.AutoReset = true;
            _endTimer.Elapsed += new System.Timers.ElapsedEventHandler(_endTimer_Elapsed);

            _searchResumeTimer = new System.Timers.Timer();
            _searchResumeTimer.Interval = 5000;
            _searchResumeTimer.Elapsed += new System.Timers.ElapsedEventHandler(_searchResumeTimer_Elapsed);

            Loaded += new RoutedEventHandler(WinMediaPlayer_Loaded);
        }

        void mediaElementMainVideo_ErrorEvent(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("mediaElementMainVideo_ErrorEvent = {0}", e));
        }

        void mediaElementMainVideo_MouseDownEvent(object sender, AxWMPLib._WMPOCXEvents_MouseDownEvent e)
        {
            System.Diagnostics.Debug.WriteLine("mediaElementMainVideo_MouseDown");

            if (OnFullScreen != null)
                OnFullScreen(this, new EventArgs());
        }

        void mediaElementMainVideo_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("PlayState = {0}", (int)e.newState));
            if (e.newState == (int)WMPLib.WMPPlayState.wmppsPlaying)
            {
                _isOpened = true;
            }
            else if (e.newState == (int)WMPLib.WMPPlayState.wmppsMediaEnded)
            {
                Stop();

                if (OnMediaEnded != null)
                    OnMediaEnded(this, new EventArgs());
            }
        }

        void mediaElementMainVideo_BufferingEnded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("mediaElementMainVideo_BufferingEnded");
        }

        void mediaElementMainVideo_MediaOpened(object sender, RoutedEventArgs e)
        {
            _isOpened = true;
            System.Diagnostics.Debug.WriteLine("mediaElementMainVideo_MediaOpened");
        }

        void mediaElementMainVideo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("mediaElementMainVideo_MouseDown");

            if (OnFullScreen != null)
                OnFullScreen(this, new EventArgs());
        }

        void WinMediaPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            SetParent(helper.Handle, _hwndParent);
        }

        public void Dispose()
        {
            try
            {
                mediaElementMainVideo.Ctlcontrols.stop();
                mediaElementMainVideo.close();
                _mediaServer.Stop();
                _mediaServer.Dispose();
            }
            catch { }
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
            _mediaPlayState = MediaPlayState.Stop;
        }

        void mediaElementMainVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            Stop();

            if (OnMediaEnded != null)
                OnMediaEnded(this, new EventArgs());
        }

        private void CalculateVolume(double volume)
        {
            double volumeStep = 100.0 / _volumeQuickSteps;
            _volume = (int)(volumeStep * volume);
        }

        public PortableDevice.PortableDeviceObject PortableObject
        {
            set
            {
                _object = value;
                _totalTime = 0;

                if (MediaContentManager.Instance.IsSearching)
                {
                    _searchResumeTimer.Stop();
                    MediaContentManager.Instance.PauseSearch();

                    while (!MediaContentManager.Instance.IsPaused)
                        Thread.Sleep(100);
                }

                if (_object != null)
                {
                    PortableDevice.PortableDeviceFile item = _object as PortableDevice.PortableDeviceFile;
                    PortableDevice.PortableDevice device = GetDevice(_object);

                    if (_mediaFileType == Managers.MediaFileType.Audio)
                        _totalTime = device.GetDuration(item);

                    if (System.IO.File.Exists(item.Path))
                    {
                        _mediaType = DeviceType.UsbStorage;
                        _source = item.Path;
                    }
                    else
                    {
                        _source = @"http://localhost:7896/";
                        _mediaType = DeviceType.PortableDevice;
                        _mediaServer.Device = GetDevice(item as PortableDevice.PortableDeviceObject);
                        _mediaServer.FileObject = item;
                    }
                }
            }
        }

        public string FileName
        {
            set
            {
                _fileName = value;
            }
            get
            {
                return _fileName;
            }
        }

        public uint Duration
        {
            get
            {
                return _duration;
            }
        }

        public int PercentProgress
        {
            get
            {
                int result = 0;

                try
                {
                    double totalTime = mediaElementMainVideo.currentMedia.duration;

                    if (Math.Abs(totalTime - 0.0) < 0.001)
                        totalTime = _totalTime / 1000;

                    if (!(Math.Abs(totalTime - 0.0) < 0.001)) // Make sure the duration is bigger than 0.0
                    {
                        double currentPosition = mediaElementMainVideo.Ctlcontrols.currentPosition;

                        if ((currentPosition < 0) || (currentPosition > totalTime))
                            currentPosition = 0;

                        _duration = (uint)currentPosition;

                        result = (int)(currentPosition / totalTime * 100.0);
                    }
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
            get 
            {
                return _volume; 
            }
            set 
            {
                CalculateVolume(value);
            }
        }

        public double MediaPlayerVolumeQuickSteps
        {
            get { return _volumeQuickSteps; }
            set { _volumeQuickSteps = value; }
        }

        public bool IsOpened
        {
            get
            {
                return _isOpened;
            }
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
                if (_mediaPlayState != Settings.MediaPlayState.Pause)
                {
                    mediaElementMainVideo.Ctlcontrols.stop();
                    mediaElementMainVideo.close();
                    mediaElementMainVideo.URL = _source;
                }

                mediaElementMainVideo.settings.rate = NominalVideoSpeed;
                mediaElementMainVideo.settings.mute = _isMuted;
                mediaElementMainVideo.Ctlcontrols.play();
                mediaElementMainVideo.settings.volume = (int)_volume;
                _mediaPlayState = MediaPlayState.Play;
                _isFastForward = true;
                _timeShift = 1;
                _stepCount = 0;
                _textBlockError.Visible = false;
                _textBlockMessage.Visible = false;
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

            if (_timeShift >= 16)
                _timeShift = 16;

            if (_isFastForward)
                shift = _timeShift * 1;
            else
                shift = _timeShift * -1;

            mediaElementMainVideo.Ctlcontrols.currentPosition += shift;

            _stepCount++;
        }

        private void _searchResumeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Timers.ElapsedEventHandler(SAFE_searchResumeTimer_Elapsed), sender, e);    
        }

        private void SAFE_searchResumeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _searchResumeTimer.Stop();

            if (!_mediaServer.IsStarted)
                MediaContentManager.Instance.ResumeSearch();
            else
                _searchResumeTimer.Start();
        }

        public void FastForward()
        {
            if (_mediaPlayState != MediaPlayState.Play)
                return;

            _isFastForward = true;

            System.Diagnostics.Debug.WriteLine("FastForward");
            Pause();
            _endTimer.Start();
        }

        public void EndFastForward()
        {
            if (!_endTimer.Enabled)
                return;

            System.Diagnostics.Debug.WriteLine("EndFastForward");
            mediaElementMainVideo.Ctlcontrols.play();
            _endTimer.Stop();
            Play();
        }

        public void Rewind()
        {
            if (_mediaPlayState != MediaPlayState.Play)
                return;

            _isFastForward = false;

            System.Diagnostics.Debug.WriteLine("Rewind");
            Pause();
            _endTimer.Start();
        }

        public void EndRewind()
        {
            if (!_endTimer.Enabled)
                return;

            System.Diagnostics.Debug.WriteLine("EndRewind");
            _endTimer.Stop();
            Play();
        }

        public void Pause()
        {
            try
            {
                mediaElementMainVideo.Ctlcontrols.pause();
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
                mediaElementMainVideo.Ctlcontrols.stop();
                mediaElementMainVideo.URL = null;
                _mediaPlayState = MediaPlayState.Stop;
                _isFastForward = true;
                _timeShift = 1;
                _stepCount = 0;
                _duration = 0;
                _totalTime = 0;
                if (_mediaType == DeviceType.PortableDevice)
                    _mediaServer.Stop();
                _isOpened = false;
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
                mediaElementMainVideo.settings.mute = mute;
                _isMuted = mute;

                if (!mute)
                    mediaElementMainVideo.settings.volume = (int)_volume;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "winmediaplayer.ismuted: exception"));
            }
        }

        public void SetWindowSize(Rect rect)
        {
            Hide();
            var helper = new WindowInteropHelper(this);
            SetParent(helper.Handle, IntPtr.Zero);
            Width = rect.Width;
            Height = rect.Height;
            SetParent(helper.Handle, _hwndParent);
            Show();
        }

        private PortableDevice.PortableDevice GetDevice(PortableDevice.PortableDeviceObject item)
        {
            PortableDevice.PortableDevice device = null;

            foreach (PortableObject obj in MediaContentManager.Instance.PortableObjectList)
            {
                if (obj.ObjectList.Contains(item.Id))
                {
                    device = obj.Device;
                    break;
                }
            }

            return device;
        }

        #region mediaserver event handle
        void _mediaServer_OnLoadingFinished(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new EventHandler(SAFE_mediaServer_OnLoadingFinished), sender, e);
        }

        private void SAFE_mediaServer_OnLoadingFinished(object sender, EventArgs e)
        {
            _searchResumeTimer.Stop();

            if (MediaContentManager.Instance.IsSearching)
                _searchResumeTimer.Start();
        }
        #endregion

        public MediaPlayState MediaPlayState
        {
            get { return _mediaPlayState; }
        }

        public MediaFileType MediaFileType
        {
            get { return _mediaFileType; }
            set { _mediaFileType = value; }
        }

        public DeviceType MediaType
        {
            get { return _mediaType; }
            set { _mediaType = value; }
        }

        public bool IsMuted
        {
            get { return _isMuted; }
        }

        public IntPtr HWNDParent
        {
            set { _hwndParent = value; }
        }

        public double VideoWidth
        {
            set
            {
                Width = value;
            }
        }

        public double VideoHeight
        {
            set
            {
                Height = value;
            }
        }
    }
}
