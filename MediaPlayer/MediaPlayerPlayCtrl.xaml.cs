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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

using MediaPlayer.Controls;
using MediaPlayer.Windows;
using MediaPlayer.Settings;
using MediaPlayer.Managers;

using JHTNA.modules.strings;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MediaPlayerPlayCtrl.xaml
    /// </summary>
    public partial class MediaPlayerPlayCtrl : UserControl
    {
        private System.Timers.Timer _progressTimer;
        private System.Timers.Timer _textScrollTimer;
        private System.Timers.Timer _bufferingTimer;
        private System.Timers.Timer _playTimer;

        private bool _showBuffering;
        private PortableDevice.PortableDeviceObject _currentObject;
        private MediaFileType _currentMediaFileType;

        #region Events
        public event EventHandler MoveNext;
        public event EventHandler MovePrevious;
        public event EventHandler ShowVideo;
        public event EventHandler StateChange;
        #endregion

        public MediaPlayerPlayCtrl()
        {
            InitializeComponent();

            _progressTimer = new System.Timers.Timer();
            _progressTimer.Interval = 1000;
            _progressTimer.Elapsed += new System.Timers.ElapsedEventHandler(_progressTimer_Elapsed);

            _textScrollTimer = new System.Timers.Timer();
            _textScrollTimer.Interval = 6000;
            _textScrollTimer.Elapsed += new System.Timers.ElapsedEventHandler(_textScrollTimer_Elapsed);

            _bufferingTimer = new System.Timers.Timer();
            _bufferingTimer.Interval = 2000;
            _bufferingTimer.Elapsed += new System.Timers.ElapsedEventHandler(_bufferingTimer_Elapsed);

            _playTimer = new System.Timers.Timer();
            _playTimer.Interval = 500;
            _playTimer.Elapsed += new System.Timers.ElapsedEventHandler(_playTimer_Elapsed);

            buttonForward.AutoRepeat = true;
            buttonBack.AutoRepeat = true;

            _showBuffering = false;
            _currentMediaFileType = MediaFileType.Audio;

            Loaded += new RoutedEventHandler(MediaPlayerPlayCtrl_Loaded);
            MediaContentManager.Instance.UpdateMedia += new EventHandler(Instance_UpdateMedia);
        }

        void Instance_UpdateMedia(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new EventHandler(SAFE_Instance_UpdateMedia), sender, e);
        }

        private void SAFE_Instance_UpdateMedia(object sender, EventArgs e)
        {
            Stop();
        }

        void MediaPlayerPlayCtrl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void _progressTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Timers.ElapsedEventHandler(SAFE_progressTimer_Elapsed), sender, e);
        }

        private void SAFE_progressTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            double pixelsPerPercent = rectangleProgressBackground.ActualWidth / 100;
#if VLC
            rectangleProgressForeground.Width = pixelsPerPercent * VLCPlayer.Instance.PercentProgress;
#else
            rectangleProgressForeground.Width = pixelsPerPercent * WinMediaPlayer.Instance.PercentProgress;
#endif
        }

        private void _textScrollTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Timers.ElapsedEventHandler(SAFE_textScrollTimer_Elapsed), sender, e);
        }

        private void SAFE_textScrollTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            bool scroll = false;
            _textScrollTimer.Stop();

            if (textBlockTitle.Text != string.Empty)
                scroll = true;

            if (scroll)
            {
                DoScroll();
                _textScrollTimer.Interval = 10000;
                _textScrollTimer.Start();
            }
        }

        private void _bufferingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Timers.ElapsedEventHandler(SAFE_bufferingTimer_Elapsed), sender, e);
        }

        private void SAFE_bufferingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _bufferingTimer.Stop();
#if VLC
            if (VLCPlayer.Instance.IsOpened)
#else
            if (WinMediaPlayer.Instance.IsOpened)
#endif
            {
#if VLC
                textBlockTitle.Text = System.IO.Path.GetFileNameWithoutExtension(VLCPlayer.Instance.FileName);
#else
                textBlockTitle.Text = System.IO.Path.GetFileNameWithoutExtension(WinMediaPlayer.Instance.FileName);
#endif
                _textScrollTimer.Interval = 1000;
                _textScrollTimer.Start();
            }
            else
            {
                if (_showBuffering)
#if VLC
                    textBlockTitle.Text = System.IO.Path.GetFileNameWithoutExtension(VLCPlayer.Instance.FileName);
#else
                    textBlockTitle.Text = System.IO.Path.GetFileNameWithoutExtension(WinMediaPlayer.Instance.FileName);
#endif
                else
                    textBlockTitle.Text = StringManager.GetAppString(StringsConstApplication.Loading);

                _showBuffering = !_showBuffering;

                _bufferingTimer.Start();
            }
        }

        private void _playTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Timers.ElapsedEventHandler(SAFE_playTimer_Elapsed), sender, e);
        }

        private void SAFE_playTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _playTimer.Stop();

            System.Diagnostics.Debug.WriteLine(string.Format("================= SAFE_playTimer_Elapsed ================"));
#if VLC
            VLCPlayer.Instance.MediaFileType = _currentMediaFileType;
            VLCPlayer.Instance.PortableObject = _currentObject;
            VLCPlayer.Instance.FileName = _currentObject.Name;
#else
            WinMediaPlayer.Instance.MediaFileType = _currentMediaFileType;
            WinMediaPlayer.Instance.PortableObject = _currentObject;
            WinMediaPlayer.Instance.FileName = _currentObject.Name;
#endif
            if (_currentObject != null)
                SetPlayState(MediaPlayState.Play);
        }

        private void buttonBack_ButtonClick(object sender, EventArgs e)
        {
            if (e is MediaPlayer.Controls.ThreeStateButtonArgs)
            {
                ThreeStateButtonArgs args = e as ThreeStateButtonArgs;

#if VLC
                if (MovePrevious != null)
                    MovePrevious(this, new EventArgs());
#else
                if (!args.IsAutoRepeat && (args.AutoRepeatCount == 0))
                {
                    if (MovePrevious != null)
                        MovePrevious(this, new EventArgs());
                }
                else if (args.IsAutoRepeat && (args.AutoRepeatCount == 0))
                {
                    SetPlayState(MediaPlayState.StartRewind);
                }
                else if (!args.IsAutoRepeat && (args.AutoRepeatCount > 0))
                {
                    SetPlayState(MediaPlayState.EndRewind);
                }
#endif
            }
        }

        private void buttonPlayPause_ButtonClick(object sender, EventArgs e)
        {
#if VLC
            if (string.IsNullOrEmpty(VLCPlayer.Instance.FileName))
#else
            if (string.IsNullOrEmpty(WinMediaPlayer.Instance.FileName))
#endif
                return;
#if VLC
            switch (VLCPlayer.Instance.MediaPlayState)
#else
            switch (WinMediaPlayer.Instance.MediaPlayState)
#endif
            { 
                case MediaPlayState.Stop:
                    SetPlayState(MediaPlayState.Play);
                    break;
                case MediaPlayState.Pause:
                    SetPlayState(MediaPlayState.Play);
                    break;
                case MediaPlayState.Play:
                    SetPlayState(MediaPlayState.Pause);
                    break;
            }
        }

        private void buttonForward_ButtonClick(object sender, EventArgs e)
        {
            if (e is MediaPlayer.Controls.ThreeStateButtonArgs)
            {
                ThreeStateButtonArgs args = e as ThreeStateButtonArgs;
#if VLC
                if (MoveNext != null)
                    MoveNext(this, new EventArgs());
#else
                if (!args.IsAutoRepeat && (args.AutoRepeatCount == 0))
                {
                    if (MoveNext != null)
                        MoveNext(this, new EventArgs());
                }
                else if (args.IsAutoRepeat && (args.AutoRepeatCount == 0))
                {
                    SetPlayState(MediaPlayState.StartFastForward);
                }
                else if (!args.IsAutoRepeat && (args.AutoRepeatCount > 0))
                {
                    SetPlayState(MediaPlayState.EndFastForward);
                }
#endif
            }
        }

        public void SetMediaFile(PortableDevice.PortableDeviceObject obj, MediaFileType mediaFileType)
        {
            Stop();

            _currentMediaFileType = mediaFileType;
            _currentObject = obj;

            _playTimer.Stop();
            _playTimer.Start();
        }

        #region EventHandlers
        private void NotifyStateChange()
        {
            if (StateChange != null)
                StateChange(this, new EventArgs());
        }
        #endregion

        public void Stop()
        {
            SetPlayState(MediaPlayState.Stop);
        }

        #region SetPlayState method
        private void SetPlayState(MediaPlayState playState)
        {
            try
            {
                switch (playState)
                { 
                    case MediaPlayState.Stop:
                        _progressTimer.Stop();
                        _textScrollTimer.Stop();
                        _bufferingTimer.Stop();
#if VLC
                        VLCPlayer.Instance.Stop();
                        VLCPlayer.Instance.Hide();
#else
                        WinMediaPlayer.Instance.Stop();
                        WinMediaPlayer.Instance.Hide();
#endif
                        rectangleProgressForeground.Width = 0;
                        rectangleProgressForeground.Visibility = System.Windows.Visibility.Hidden;
                        rectangleProgressBackground.Visibility = System.Windows.Visibility.Hidden;
                        textBlockTitle.Text = string.Empty;
#if VLC
                        VLCPlayer.Instance.FileName = string.Empty;
#else
                        WinMediaPlayer.Instance.FileName = string.Empty;
#endif
                        NotifyStateChange();
                        break;
                    case MediaPlayState.Pause:
                        _textScrollTimer.Stop();
                        textBlockTitle.BeginAnimation(Canvas.LeftProperty, null);
                        rectangleProgressForeground.Visibility = System.Windows.Visibility.Visible;
                        rectangleProgressBackground.Visibility = System.Windows.Visibility.Visible;
#if VLC
                        VLCPlayer.Instance.Pause();
                        VLCPlayer.Instance.Hide();
#else
                        WinMediaPlayer.Instance.Pause();
                        WinMediaPlayer.Instance.Hide();
#endif
                        NotifyStateChange();
                        break;
                    case MediaPlayState.Play:
                        rectangleProgressForeground.Visibility = System.Windows.Visibility.Visible;
                        rectangleProgressBackground.Visibility = System.Windows.Visibility.Visible;
#if VLC
                        VLCPlayer.Instance.Play();
                        if ((ShowVideo != null) && (VLCPlayer.Instance.MediaFileType == MediaFileType.Video))
                            ShowVideo(this, new EventArgs());

                        textBlockTitle.Text = System.IO.Path.GetFileNameWithoutExtension(VLCPlayer.Instance.FileName);
#else
                        WinMediaPlayer.Instance.Play();
                        // Show Video
                        if ((ShowVideo != null) && (WinMediaPlayer.Instance.MediaFileType == MediaFileType.Video))
                            ShowVideo(this, new EventArgs());

                        textBlockTitle.Text = System.IO.Path.GetFileNameWithoutExtension(WinMediaPlayer.Instance.FileName);
#endif
                        _progressTimer.Start();
                        textBlockTitle.BeginAnimation(Canvas.LeftProperty, null);
                        _bufferingTimer.Start();
                        NotifyStateChange();
                        break;
                    case MediaPlayState.StartFastForward:
#if VLC
                        
#else
                        WinMediaPlayer.Instance.FastForward();
#endif
                        break;
                    case MediaPlayState.EndFastForward:
#if VLC
#else
                        WinMediaPlayer.Instance.EndFastForward();
#endif
                        break;
                    case MediaPlayState.StartRewind:
#if VLC
#else
                        WinMediaPlayer.Instance.Rewind();
#endif
                        break;
                    case MediaPlayState.EndRewind:
#if VLC
#else
                        WinMediaPlayer.Instance.EndRewind();
#endif
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "Failed to set media element state!"));
            }
        }
        #endregion

        private void DoScroll()
        {
            textBlockTitle.BeginAnimation(Canvas.LeftProperty, null);

            DoubleAnimation doubleAnimation = new DoubleAnimation();
            doubleAnimation.From = canMain.ActualWidth;
            doubleAnimation.To = -(textBlockTitle.ActualWidth + 23);
            doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
            doubleAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(10000));
            textBlockTitle.BeginAnimation(Canvas.LeftProperty, doubleAnimation);
        }
    }
}
