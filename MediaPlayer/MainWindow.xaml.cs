﻿using System;
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
using System.Windows.Interop;

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

        private double _left = 0;
        private double _top = 300;

        private bool _isStandalone = true;

        private bool _isFullScreen = false;

        #region Named Pipe
        private const string PIPE_NAME = "namemediaplayerpipe";
        private PipeServer _pipeServer;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            ProcessArgs(Environment.GetCommandLineArgs());

            if (!_isStandalone)
            {
//                this.SetResourceReference(DataContextProperty, "CustomStyle");

                this.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
                this.Left = _left;
                this.Top = _top;

                _pipeServer = new PipeServer();
                _pipeServer.PipeMessage += new DelegateMessage(_pipeServer_PipeMessage);
                _pipeServer.VisibleEvent += new EventHandler(_pipeServer_VisibleEvent);
                _pipeServer.Listen(PIPE_NAME);
            }
            else
            {
                this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                this.ResizeMode = System.Windows.ResizeMode.CanResize;
//                this.SetResourceReference(DataContextProperty, "DefaultStyle");
            }

            mediaPlayerListCtrl.ItemSelectionHandler += new MediaPlayerListCtrl.ItemSelection(mediaPlayerListCtrl_ItemSelectionHandler);

            mediaPlayerPlayCtrl.MoveNext += new EventHandler(mediaPlayerPlayCtrl_MoveNext);
            mediaPlayerPlayCtrl.MovePrevious += new EventHandler(mediaPlayerPlayCtrl_MovePrevious);
            mediaPlayerPlayCtrl.ShowVideo += new EventHandler(mediaPlayerPlayCtrl_ShowVideo);
            mediaPlayerPlayCtrl.StateChange += new EventHandler(mediaPlayerPlayCtrl_StateChange);
#if VLC
            VLCPlayer.Instance.OnMediaEnded += new EventHandler(Instance_OnMediaEnded);
#else
            WinMediaPlayer.Instance.MouseDown += new MouseButtonEventHandler(Instance_MouseDown);
            WinMediaPlayer.Instance.OnMediaEnded += new EventHandler(Instance_OnMediaEnded);
            WinMediaPlayer.Instance.OnFullScreen += new EventHandler(Instance_OnFullScreen);
#endif
            Loaded += new RoutedEventHandler(MainWindow_Loaded);
            Closed += new EventHandler(MainWindow_Closed);

            SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
        }

        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
#if VLC
            VLCPlayer.Instance.Width = mediaPlayerListCtrl.ActualWidth;
            VLCPlayer.Instance.Height = mediaPlayerListCtrl.ActualHeight;
#else
            WinMediaPlayer.Instance.Width = mediaPlayerListCtrl.ActualWidth;
            WinMediaPlayer.Instance.Height = mediaPlayerListCtrl.ActualHeight;
#endif
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

        void Instance_OnFullScreen(object sender, EventArgs e)
        {
            double width;
            double height;

            if (!_isFullScreen)
            {
                width = this.Width;
                height = this.Height;
                _isFullScreen = true;
            }
            else
            {
                width = mediaPlayerListCtrl.ActualWidth;
                height = mediaPlayerListCtrl.ActualHeight;
                _isFullScreen = false;
            }

            WinMediaPlayer.Instance.Width = width;
            WinMediaPlayer.Instance.Height = height;
        }

        void mediaPlayerPlayCtrl_StateChange(object sender, EventArgs e)
        {
            // Audio & Video Implement
#if VLC
            if (VLCPlayer.Instance.MediaPlayState == MediaPlayState.Play)
#else
            if (WinMediaPlayer.Instance.MediaPlayState == MediaPlayState.Play)
#endif
            { 
                
            }

            _mediaEnded = false;
        }

        void mediaPlayerPlayCtrl_ShowVideo(object sender, EventArgs e)
        {
#if VLC
            VLCPlayer.Instance.VideoWidth = mediaPlayerListCtrl.ActualWidth;
            VLCPlayer.Instance.VideoHeight = mediaPlayerListCtrl.ActualHeight;
            VLCPlayer.Instance.Show();
#else
            WinMediaPlayer.Instance.VideoWidth = mediaPlayerListCtrl.ActualWidth;
            WinMediaPlayer.Instance.VideoHeight = mediaPlayerListCtrl.ActualHeight;
            WinMediaPlayer.Instance.Show();
#endif
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
            mediaPlayerPlayCtrl.Stop();
            mediaPlayerListCtrl.StopControl();
#if VLC
            VLCPlayer.Instance.StopPlayer();
#else
            WinMediaPlayer.Instance.Dispose();
#endif
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isStandalone)
            {
                Hide();

                IntPtr hostHandle = HookWindows.Instance.GetHostHandle("picard", "winworkout");
                IntPtr guestHandle = new WindowInteropHelper(this).Handle;

                if ((hostHandle != IntPtr.Zero) && (guestHandle != IntPtr.Zero))
                {
                    HookWindows.Instance.HookMainWindow(hostHandle, guestHandle);
                }

                Show();
            }

            AddInsertUSBHandler();
            AddRemoveUSBHandler();
            MediaContentManager.Instance.Init();          
        }

        public void MuteAudio(bool mute)
        {
#if VLC
            VLCPlayer.Instance.Mute(mute);
#else
            WinMediaPlayer.Instance.Mute(mute);
#endif
        }

        public void StopVideo()
        {
#if VLC
            if ((VLCPlayer.Instance.MediaFileType == MediaFileType.Video) &&
                (VLCPlayer.Instance.MediaPlayState != MediaPlayState.Stop))
#else
            if ((WinMediaPlayer.Instance.MediaFileType == MediaFileType.Video) &&
                (WinMediaPlayer.Instance.MediaPlayState != MediaPlayState.Stop))
#endif
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

        #region Named Pipe
        private void _pipeServer_VisibleEvent(object sender, EventArgs e)
        {
            _pipeServer.IsBrowserVisible = (this.Visibility == System.Windows.Visibility.Visible); 
        }

        private void _pipeServer_PipeMessage(string message)
        {
            ShowPipeMessage(message);

            try
            {
                if (message.ToLower().Contains("hide"))
                {
                    Dispatcher.BeginInvoke(new Action(HideWindow));
                }
                else if (message.ToLower().Contains("show"))
                {
                    Dispatcher.BeginInvoke(new Action(ShowWindow));
                }
                else if (message.ToLower().Contains("stop"))
                {
                    Dispatcher.BeginInvoke(new Action(StopWindow));
                }
            }
            catch (Exception ex)
            {
                ShowException(
                    ex,
                    "_pipeServer_PipeMessage : exception!");
            }
        }

        private void ShowPipeMessage(string message)
        {
            string dateTime = "[" + DateTime.Now.ToShortDateString() + " " +
                DateTime.Now.ToLongTimeString() + "] ";

            System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                dateTime,
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                message));
        }

        private void ShowException(Exception ex, string message)
        {
            string dateTime = "[" + DateTime.Now.ToShortDateString() + " " +
                DateTime.Now.ToLongTimeString() + "] ";

            System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2} {3}",
                dateTime,
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                ex,
                message));

        }
        #endregion

        #region Window Control
        private void HideWindow()
        {
            this.Hide();
        }

        private void ShowWindow()
        {
            this.Show();
            this.Activate();
            this.Focus();
        }

        private void StopWindow()
        {
            Close();
            App.Current.Shutdown();
        }
        #endregion

        #region Process Args
        private double ExtractNumber(string original)
        {
            int result = 0;

            int.TryParse(new string(original.Where(c => Char.IsDigit(c)).ToArray()), out result);

            return result;
        }

        private void ProcessArgs(string[] args)
        {
            if (args.Count() <= 1)
                return;

            this._isStandalone = false;

            foreach (string arg in args)
            {
                if (arg.ToLower().Contains("left="))
                {
                    _left = ExtractNumber(arg.ToLower());
                }
                else if (arg.ToLower().Contains("top="))
                {
                    _top = ExtractNumber(arg.ToLower());
                }
            }
        }
        #endregion
    }
}
