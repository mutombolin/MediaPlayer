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

using System.Management;

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
        private ManagementEventWatcher _watcherRemove;
        private ManagementEventWatcher _watcherInsert;

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

            SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
        }

        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            WinMediaPlayer.Instance.Width = mediaPlayerListCtrl.ActualWidth;
            WinMediaPlayer.Instance.Height = mediaPlayerListCtrl.ActualHeight;
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
//            Point topLeft = mediaPlayerListCtrl.PointToScreen(new Point(0, 0));

//            WinMediaPlayer.Instance.Left = topLeft.X;
//            WinMediaPlayer.Instance.Top = topLeft.Y;
//            WinMediaPlayer.Instance.Width = mediaPlayerListCtrl.ActualWidth;
//            WinMediaPlayer.Instance.Height = mediaPlayerListCtrl.ActualHeight;
            WinMediaPlayer.Instance.VideoWidth = mediaPlayerListCtrl.ActualWidth;
            WinMediaPlayer.Instance.VideoHeight = mediaPlayerListCtrl.ActualHeight;

            WinMediaPlayer.Instance.Show();
        }

        void mediaPlayerPlayCtrl_MovePrevious(object sender, EventArgs e)
        {
            mediaPlayerListCtrl.MovePrevious();
        }

        void mediaPlayerPlayCtrl_MoveNext(object sender, EventArgs e)
        {
            mediaPlayerListCtrl.MoveNext();
        }

        void mediaPlayerListCtrl_ItemSelectionHandler(PortableDevice.PortableDeviceObject obj, MediaFileType mediaFileType)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("filename = {0}", obj.Name));
            mediaPlayerPlayCtrl.SetMediaFile(obj, mediaFileType);
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            mediaPlayerListCtrl.StopControl();
            WinMediaPlayer.Instance.Dispose();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AddInsertUSBHandler();
            AddRemoveUSBHandler();
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

        #region USB Insert/Remove detect
        private void AddRemoveUSBHandler()
        {
            WqlEventQuery q;
            ManagementScope scope = new ManagementScope("root\\CIMV2");
            scope.Options.EnablePrivileges = true;

            try
            {
                q = new WqlEventQuery();
                q.EventClassName = "__InstanceDeletionEvent";
                q.WithinInterval = new TimeSpan(0, 0, 5);
                q.Condition = "TargetInstance ISA 'Win32_USBControllerdevice'";
                _watcherRemove = new ManagementEventWatcher(scope, q);
                _watcherRemove.EventArrived += new EventArrivedEventHandler(_watcher_EventArrived);
                _watcherRemove.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "AddUSBHandler - Exception!!"));

                if (_watcherRemove != null)
                    _watcherRemove.Stop();
            }
        }

        private void AddInsertUSBHandler()
        {
            WqlEventQuery q;
            ManagementScope scope = new ManagementScope("root\\CIMV2");
            scope.Options.EnablePrivileges = true;

            try
            {
                q = new WqlEventQuery();
                q.EventClassName = "__InstanceCreationEvent";
                q.WithinInterval = new TimeSpan(0, 0, 5);
                q.Condition = "TargetInstance ISA 'Win32_USBControllerdevice'";
                _watcherInsert = new ManagementEventWatcher(scope, q);
                _watcherInsert.EventArrived += new EventArrivedEventHandler(_watcherInsert_EventArrived);

                _watcherInsert.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "AddInsertUSBHandler - Exception!!"));

                if (_watcherInsert != null)
                    _watcherInsert.Stop();
            }
        }

        void _watcherInsert_EventArrived(object sender, EventArrivedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("A USB device Inserted");
            MediaContentManager.Instance.Init();
        }

        void _watcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("A USB device removed");
            MediaContentManager.Instance.Init();
        }
        #endregion
    }
}
