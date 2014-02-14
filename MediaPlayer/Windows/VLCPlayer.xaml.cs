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
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;

using MediaPlayer.Libs;
using MediaPlayer.Settings;
using MediaPlayer.Managers;
using MediaPlayer.Data;
using MediaPlayer.Server;

namespace MediaPlayer.Windows
{
    /// <summary>
    /// Interaction logic for VLCPlayer.xaml
    /// </summary>
    public partial class VLCPlayer : Window
    {
        private double _volume;
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
        private long _duration;
        private string _source = string.Empty;

        private IntPtr _hwndParent;

        public static readonly VLCPlayer Instance = new VLCPlayer();

        public event EventHandler OnMediaEnded;

        private HttpServer _mediaServer;

        [DllImport("User32.dll")]
        static extern IntPtr SetParent(IntPtr hWnd, IntPtr hParent);

        private IntPtr _instance, _player, _media;

        private long _tick = 0;
        
        // Event Handler
        private LibVlcAPI.EventCallbackDelegate _pausedDelegate;
        private LibVlcAPI.EventCallbackDelegate _playingDelegate;
        private LibVlcAPI.EventCallbackDelegate _endReachedDelegate;
        private LibVlcAPI.EventCallbackDelegate _openingDelegate;
        private LibVlcAPI.EventCallbackDelegate _stateChangedDelegate;

        public VLCPlayer()
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
            _volume = 0;
            _isOpened = false;
            _duration = 0;
            _instance = IntPtr.Zero;
            _player = IntPtr.Zero;
            _media = IntPtr.Zero;

            // Server Start here maybe.

            // start vlc
//            string[] arguments = { "-I", "dummy", "--ignore-config", "--no-video-title"};
            string[] arguments = { "-I", "dummy", "--ignore-config", "--no-video-title", "--no-keyboard-events", "--no-mouse-events" };
            _instance = LibVlcAPI.libvlc_new(arguments.Length, arguments);
            _player = LibVlcAPI.libvlc_media_player_new(_instance);

            IntPtr eventManager = LibVlcAPI.libvlc_media_player_event_manager(_player);

            _pausedDelegate = new LibVlcAPI.EventCallbackDelegate(MediaPlayerPaused);
            _playingDelegate = new LibVlcAPI.EventCallbackDelegate(MediaPlayerPlaying);
            _endReachedDelegate = new LibVlcAPI.EventCallbackDelegate(MediaPlayerEndReached);
            _openingDelegate = new LibVlcAPI.EventCallbackDelegate(MediaPlayerOpening);
            _stateChangedDelegate = new LibVlcAPI.EventCallbackDelegate(MediaPlayerStateChanged);
            

            LibVlcAPI.libvlc_event_attach(eventManager, LibVlcAPI.libvlc_event_type_t.libvlc_MediaPlayerPaused, _pausedDelegate, IntPtr.Zero);
            LibVlcAPI.libvlc_event_attach(eventManager, LibVlcAPI.libvlc_event_type_t.libvlc_MediaPlayerPlaying, _playingDelegate, IntPtr.Zero);
            LibVlcAPI.libvlc_event_attach(eventManager, LibVlcAPI.libvlc_event_type_t.libvlc_MediaPlayerEndReached, _endReachedDelegate, IntPtr.Zero);
            LibVlcAPI.libvlc_event_attach(eventManager, LibVlcAPI.libvlc_event_type_t.libvlc_MediaPlayerOpening, _openingDelegate, IntPtr.Zero);
            LibVlcAPI.libvlc_event_attach(eventManager, LibVlcAPI.libvlc_event_type_t.libvlc_MediaStateChanged, _stateChangedDelegate, IntPtr.Zero);

            Loaded += new RoutedEventHandler(VLCPlayer_Loaded);
        }

        #region vlc event delegate
        private void MediaPlayerPaused(IntPtr userdata)
        {
            System.Diagnostics.Debug.WriteLine("VLC Paused");
        }

        private void MediaPlayerPlaying(IntPtr userdata)
        { 
            System.Diagnostics.Debug.WriteLine("VLC Playing");
            _isOpened = true;
        }

        private void MediaPlayerEndReached(IntPtr userdata)
        {
            System.Diagnostics.Debug.WriteLine("End Reached");

            Dispatcher.BeginInvoke(new LibVlcAPI.EventCallbackDelegate(SAFE_MediaPlayerEndReached), userdata);
        }

        private void SAFE_MediaPlayerEndReached(IntPtr userdata)
        {
            Stop();

            if (OnMediaEnded != null)
                OnMediaEnded(this, new EventArgs());        
        }

        private void MediaPlayerOpening(IntPtr userdata)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("MediaPlayerOpening - {0}", userdata));
        }

        private void MediaPlayerStateChanged(IntPtr userdata)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("MediaPlayerStateChanged - {0}", userdata));
        }
        #endregion

        public void StopPlayer()
        {
            Stop();

            try
            {
                while (_media != IntPtr.Zero)
                {
                    LibVlcAPI.libvlc_media_release(_media);
                    Thread.Sleep(1000);
                }

                while (_player != IntPtr.Zero)
                {
                    LibVlcAPI.libvlc_media_player_release(_player);
                    Thread.Sleep(1000);
                }
                while (_instance != IntPtr.Zero)
                {
                    LibVlcAPI.libvlc_release(_instance);
                    Thread.Sleep(1000);
                }
                _player = IntPtr.Zero;
                _instance = IntPtr.Zero;
            }
            catch { }

            Environment.Exit(0);
        }

        void VLCPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            SetParent(helper.Handle, _hwndParent);

            HwndSource hwnd = (HwndSource)HwndSource.FromVisual(this);
            LibVlcAPI.libvlc_media_player_set_hwnd(_player, (Int32)hwnd.Handle);
        }

        #region Player control methods
        public void Play()
        {
            try
            {
                // Play
                if (_mediaPlayState != Settings.MediaPlayState.Pause)
                    SetSource();

                LibVlcAPI.libvlc_media_player_play(_player);

                _mediaPlayState = Settings.MediaPlayState.Play;
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
                    "vlcplayer.play: exception"));
            }               
        }

        public void FastForward()
        {
            if (_mediaPlayState != MediaPlayState.Play)
                return;

            _isFastForward = true;
        }

        public void EndFastForward()
        { 
            
        }

        public void Rewind()
        { 
        
        }

        public void EndRewind()
        { 
        
        }

        public void Pause()
        {
            try
            {
                if (_player != IntPtr.Zero)
                    LibVlcAPI.libvlc_media_player_pause(_player);

                _mediaPlayState = Settings.MediaPlayState.Pause;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "vlcplayer.pause: exception"));            
            }
        }

        public void Stop()
        {
            try
            {
                if (_player != IntPtr.Zero)
                    LibVlcAPI.libvlc_media_player_stop(_player);

                StopServer();
                _isOpened = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "vlcplayer.stop: exception"));   
            }
        }

        public void Mute(bool mute)
        {
            try
            {

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "vlcplayer.ismuted: exception"));            
            }
        }

        public void SetSource()
        {
            System.Diagnostics.Debug.WriteLine(string.Format("SetSource -- Start {0}", GetTicks(true)));
            StopServer();

            IntPtr media;

            if (_mediaType == DeviceType.PortableDevice)
            {
                StartServer();
                media = LibVlcAPI.libvlc_media_new_location(_instance, _source);
            }
            else
            {
                media = LibVlcAPI.libvlc_media_new_path(_instance, _source);
            }

//            _media = LibVlcAPI.libvlc_media_new_path(_instance, _source);

            if (media != IntPtr.Zero)
            {
                LibVlcAPI.libvlc_media_parse(media);

                _duration = LibVlcAPI.libvlc_media_get_duration(media);

                LibVlcAPI.libvlc_media_player_set_media(_player, media);

                LibVlcAPI.libvlc_media_release(media);
            }

            System.Diagnostics.Debug.WriteLine(string.Format("SetSource -- Start {0}", GetTicks(false)));
        }
        #endregion

        #region Portable Device Methods
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
        #endregion

        #region Properties
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

        public PortableDevice.PortableDeviceObject PortableObject
        {
            set
            {
                _object = value;

                if (_object != null)
                {
                    PortableDevice.PortableDeviceFile item = _object as PortableDevice.PortableDeviceFile;
                    System.Diagnostics.Debug.WriteLine(string.Format("path = {0}", item.Path));

                    if (System.IO.File.Exists(item.Path))
                    {
                        _mediaType = DeviceType.UsbStorage;
                        _source = item.Path;
                    }
                    else
                    {
                        _source = @"http://localhost:7896/";
                        _mediaType = DeviceType.PortableDevice;                        
                    }
                }
            }
        }

        public string FileName
        {
            set { _fileName = value; }
            get { return _fileName; }
        }

        public int PercentProgress
        {
            get
            {
                int result = 0;

                try
                {
                    if ((_instance == IntPtr.Zero) || (_player == IntPtr.Zero))
                        return result;

                    if (_duration <= 0)
                    {
                        if (_player != IntPtr.Zero)
                        {
//                            LibVlcAPI.libvlc_media_parse(_media);
                            _duration = LibVlcAPI.libvlc_media_player_get_length(_player);

//                            _duration = LibVlcAPI.libvlc_media_get_duration(_media);
                        }
                    }

                    long current = 0;
                    current = LibVlcAPI.libvlc_media_player_get_time(_player);

                    if ((current != 0) && (_duration != 0))
                    {
                        result = (int)(Convert.ToDouble(current) / Convert.ToDouble(_duration) * 100.0);
                    }

                    System.Diagnostics.Debug.WriteLine(string.Format("result = {0}", result));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                        System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                        ex,
                        "Failed to set UI state from vlc status!"));
                }

                return result;
            }
        }

        public double MediaPlayerVolume
        {
            get { return _volume; }
        }

        public bool IsOpened
        {
            get { return _isOpened; }
        }

        public IntPtr HWNDParent
        {
            set { _hwndParent = value; }
        }

        public double VideoWidth
        {
            set { Width = value; }
        }

        public double VideoHeight
        {
            set { Height = value; }
        }
        #endregion

        #region Server
        private void StartServer()
        {
            _mediaServer = new HttpServer(7896, (req) =>
                {
                    return GetVideo();
                });
        }

        private void StopServer()
        {
            if (_mediaServer != null)
            {
                _mediaServer.Stop();
                _mediaServer = null;
            }
        }

        private CompactResponse GetVideo()
        {
            if (_object != null)
            {
                PortableDevice.PortableDeviceFile item = _object as PortableDevice.PortableDeviceFile;
                PortableDevice.PortableDevice device = GetDevice(item as PortableDevice.PortableDeviceObject);
                MemoryStream ms = device.GetMemoryStream(item);

                return new CompactResponse()
                {
                    ContentType = "video/mp4",
                    DataMS = ms
                };
            }
            else
            {
                return new CompactResponse()
                {
                    StatusText = CompactResponse.HttpStatus.HTTP404
                };
            }
        }
        #endregion

        private long GetTicks(bool reset)
        {
            long result = 0;

            if (reset)
            {
                result = DateTime.Now.Ticks;
                _tick = result;
            }
            else
            {
                result = DateTime.Now.Ticks - _tick;
            }

            return result;
        }
    }
}
