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

using MediaPlayer.Common;
using MediaPlayer.Settings;
using MediaPlayer.Windows;
using MediaPlayer.Managers;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _mediaEnded = false;

        public MainWindow()
        {
            InitializeComponent();

            mediaPlayerListCtrl.ItemSelectionHandler += new MediaPlayerListCtrl.ItemSelection(mediaPlayerListCtrl_ItemSelectionHandler);

            mediaPlayerPlayCtrl.MoveNext += new EventHandler(mediaPlayerPlayCtrl_MoveNext);
            mediaPlayerPlayCtrl.MovePrevious += new EventHandler(mediaPlayerPlayCtrl_MovePrevious);
            mediaPlayerPlayCtrl.ShowVideo += new EventHandler(mediaPlayerPlayCtrl_ShowVideo);
            mediaPlayerPlayCtrl.StateChange += new EventHandler(mediaPlayerPlayCtrl_StateChange);

            WinMediaPlayer.Instance.MouseDown += new MouseButtonEventHandler(Instance_MouseDown);
            WinMediaPlayer.Instance.OnMediaEnded += new EventHandler(Instance_OnMediaEnded);

            Loaded += new RoutedEventHandler(MainWindow_Loaded);
            Closed += new EventHandler(MainWindow_Closed);
        }

        void Instance_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        void Instance_OnMediaEnded(object sender, EventArgs e)
        {
            _mediaEnded = true;
            mediaPlayerListCtrl.MoveNext();
        }

        void mediaPlayerPlayCtrl_StateChange(object sender, EventArgs e)
        {
            // Audio & Video Implement
            if (WinMediaPlayer.Instance.MediaPlayState == MediaPlayState.Play)
            { 
                
            }

            _mediaEnded = false;
        }

        void mediaPlayerPlayCtrl_ShowVideo(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        void mediaPlayerPlayCtrl_MovePrevious(object sender, EventArgs e)
        {
            mediaPlayerListCtrl.MovePrevious();
        }

        void mediaPlayerPlayCtrl_MoveNext(object sender, EventArgs e)
        {
            mediaPlayerListCtrl.MoveNext();
        }

        void mediaPlayerListCtrl_ItemSelectionHandler(string filename)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("filename = {0}", filename));
            mediaPlayerPlayCtrl.SetMediaFile(filename);
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            mediaPlayerListCtrl.StopControl();
            WinMediaPlayer.Instance.Dispose();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MediaContentManager.Instance.Init();
        }

        public void MuteAudio(bool mute)
        {
            WinMediaPlayer.Instance.Mute(mute);
        }

        public void StopVideo()
        {
            if ((WinMediaPlayer.Instance.MediaFileType == MediaFileType.Video) &&
                (WinMediaPlayer.Instance.MediaPlayState != MediaPlayState.Stop))
            {
                mediaPlayerPlayCtrl.Stop();
            }
        }

        public void StopControl()
        { 
        
        }

        public void PauseControl()
        { 
        
        }

        public void StartControl()
        { 
        
        }

        public void ResumeControl()
        { }

    }
}
