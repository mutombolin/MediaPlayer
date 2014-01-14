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

using MediaPlayer.Controls;
using MediaPlayer.Windows;
using MediaPlayer.Settings;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MediaPlayerPlayCtrl.xaml
    /// </summary>
    public partial class MediaPlayerPlayCtrl : UserControl
    {
        private System.Timers.Timer _progressTimer;

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

            buttonForward.AutoRepeat = true;
            buttonBack.AutoRepeat = true;
        }

        private void _progressTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Timers.ElapsedEventHandler(SAFE_progressTimer_Elapsed), sender, e);
        }

        private void SAFE_progressTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            double pixelsPerPercent = rectangleProgressBackground.ActualWidth / 100;
            rectangleProgressForeground.Width = pixelsPerPercent * WinMediaPlayer.Instance.PercentProgress;
        }

        private void buttonBack_ButtonClick(object sender, EventArgs e)
        {
            if (e is MediaPlayer.Controls.ThreeStateButtonArgs)
            {
                ThreeStateButtonArgs args = e as ThreeStateButtonArgs;

                if (MovePrevious != null)
                    MovePrevious(this, new EventArgs());
            }
        }

        private void buttonPlayPause_ButtonClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(WinMediaPlayer.Instance.FileName))
                return;

            switch (WinMediaPlayer.Instance.MediaPlayState)
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

                if (MoveNext != null)
                    MoveNext(this, new EventArgs());
            }
        }

        public void SetMediaFile(string mediaFileName)
        {
            Stop();
            WinMediaPlayer.Instance.FileName = mediaFileName;

            if (!string.IsNullOrEmpty(mediaFileName))
                SetPlayState(MediaPlayState.Play);
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
                        WinMediaPlayer.Instance.Stop();
                        WinMediaPlayer.Instance.Hide();
                        rectangleProgressForeground.Width = 0;
                        rectangleProgressForeground.Visibility = System.Windows.Visibility.Hidden;
                        rectangleProgressBackground.Visibility = System.Windows.Visibility.Hidden;
                        textBlockTitle.Text = string.Empty;
                        WinMediaPlayer.Instance.FileName = string.Empty;
                        NotifyStateChange();
                        break;
                    case MediaPlayState.Pause:
                        rectangleProgressForeground.Visibility = System.Windows.Visibility.Visible;
                        rectangleProgressBackground.Visibility = System.Windows.Visibility.Visible;
                        WinMediaPlayer.Instance.Pause();
                        WinMediaPlayer.Instance.Hide();
                        NotifyStateChange();
                        break;
                    case MediaPlayState.Play:
                        rectangleProgressForeground.Visibility = System.Windows.Visibility.Visible;
                        rectangleProgressBackground.Visibility = System.Windows.Visibility.Visible;
                        WinMediaPlayer.Instance.Play();
                        // Show Video
                        textBlockTitle.Text = System.IO.Path.GetFileNameWithoutExtension(WinMediaPlayer.Instance.FileName);
                        _progressTimer.Start();
                        NotifyStateChange();
                        break;
                    case MediaPlayState.StartFastForward:
                        break;
                    case MediaPlayState.EndFastForward:
                        break;
                    case MediaPlayState.StartRewind:
                        break;
                    case MediaPlayState.EndRewind:
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
    }
}
