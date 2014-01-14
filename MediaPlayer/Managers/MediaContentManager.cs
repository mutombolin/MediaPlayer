using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Threading;

namespace MediaPlayer.Managers
{
    public enum MediaFileType
    {
        Audio,
        Video
    }

    public class MediaContentManager
    {
        private int _devicesDetected = 0;
        private bool _stopThread = false;
        private Thread _deviceSearchThread = null;
        private bool _isRunning = false;
        private PortableDevice.PortableDevice _currentDevice;
        private List<PortableDevice.PortableDevice> _portableList = new List<PortableDevice.PortableDevice>();
        private List<string> _musicExtList = new List<string>();
        private List<string> _videoExtList = new List<string>();
        private Dictionary<string, PortableDevice.PortableDeviceObject> _musicPlayDictionary = new Dictionary<string, PortableDevice.PortableDeviceObject>();
        private Dictionary<string, PortableDevice.PortableDeviceObject> _videoPlayDictionary = new Dictionary<string, PortableDevice.PortableDeviceObject>();

        private List<PortableDevice.PortableDeviceObject> _musicPlayList = new List<PortableDevice.PortableDeviceObject>();
        private List<PortableDevice.PortableDeviceObject> _videoPlayList = new List<PortableDevice.PortableDeviceObject>();

        private List<string> _filterList = new List<string>();

        private bool _anyReady = false;
        private bool _isSearchingMusic = false;
        private bool _isSearchingVideo = false;

        #region Events
        public event EventHandler MediaFound;
        public event EventHandler NoMediaFound;
        public event EventHandler ScanningMedia;
        #endregion

        public static readonly MediaContentManager Instance = new MediaContentManager();

        private MediaContentManager()
        {
        }

        static MediaContentManager()
        {
        }

        #region Properties
        public Dictionary<string, PortableDevice.PortableDeviceObject> MusicPlayDictionary
        {
            get { return this._musicPlayDictionary; }
        }

        public Dictionary<string, PortableDevice.PortableDeviceObject> VideoPlayDictionary
        {
            get { return this._videoPlayDictionary; }
        }

        public List<PortableDevice.PortableDeviceObject> MusicPlayList
        {
            get { return this._musicPlayList; }
        }

        public List<PortableDevice.PortableDeviceObject> VideoPlayList
        {
            get { return this._videoPlayList; }
        }

        public bool IsRunning
        {
            get { return this._isRunning; }
        }

        public bool IsSearchingMusic
        {
            get { return this._isSearchingMusic; }
        }

        public bool IsSearchingVideo
        {
            get { return this._isSearchingVideo; }
        }

        public bool IsMusicFilesFound
        {
            get
            {
                if (this._isSearchingMusic == false)
                {
                    if (this._musicPlayList.Count > 0)
                        return true;
                }

                return false;
            }
        }

        public bool IsVideoFilesFound
        {
            get
            {
                if (this._isSearchingVideo == false)
                {
                    if (this._videoPlayList.Count > 0)
                        return true;
                }

                return false;
            }
        }

        public PortableDevice.PortableDevice Device
        {
            get { return _currentDevice; }
        }
        #endregion

        #region Init method
        public void Init()
        {
            this._devicesDetected = 0;

            this._musicExtList.Add("mp3");
            this._musicExtList.Add("wav");
            this._musicExtList.Add("aiff");
            this._musicExtList.Add("wma");

            this._videoExtList.Add("mpg");
            this._videoExtList.Add("mp4");
            this._videoExtList.Add("mov");
            this._videoExtList.Add("wmv");
            this._videoExtList.Add("avi");

            this._stopThread = false;
            this._deviceSearchThread = new Thread(new ThreadStart(MonitorForContentWorker));
            this._deviceSearchThread.Name = "MediaContentManager.MonitorForContent";
            this._deviceSearchThread.Start();
        }
        #endregion

        #region Stop method
        public void Stop()
        {
            this._stopThread = true;
        }
        #endregion

        #region MonitorForContentWorker thread
        private void MonitorForContentWorker()
        {
            try
            {
                this._filterList.Clear();
                this._filterList.Add("VA");

                this._isRunning = true;

                while (!this._stopThread)
                {
                    try
                    {
                        this._anyReady = false;
                        int deviceCount = 0;
                        var devices = new PortableDevice.PortableDeviceCollection();
                        devices.Refresh();

                        deviceCount = devices.Count;

                        if (deviceCount != this._devicesDetected)
                        {
                            if (deviceCount > 0)
                            {
                                foreach (PortableDevice.PortableDevice device in devices)
                                {
                                    device.Connect();
                                    if (!this._filterList.Contains(device.FriendlyName))
                                        _portableList.Add(device);
                                    device.Disconnect();
                                }

                                this._devicesDetected = deviceCount;
                            }

                            if (_portableList.Count > 0)
                            {
                                OnScanningMedia(this, new EventArgs());

                                this._anyReady = true;

                                _currentDevice = devices[deviceCount - 1];

//                                foreach (PortableDevice.PortableDevice device in devices)
//                                {
                                GetMusicVideoFiles(_currentDevice);
//                                }
                            }

//                            if ((this._musicPlayDictionary.Count > 0) || (this._videoPlayDictionary.Count > 0))
                            if ((this._musicPlayList.Count > 0) || (this._videoPlayList.Count > 0))
                                OnMediaFound(this, new EventArgs());

                            if (!this._anyReady)
                            {
                                this._portableList.Clear();
                                this._musicPlayDictionary.Clear();
                                this._videoPlayDictionary.Clear();

                                OnNoMediaFound(this, new EventArgs());
                            }
                        }
                    }
                    catch (Exception ex)
                    { 
                        System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                            System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                            ex,
                            "MediaContentManager.MonitorForContentWorker Exception outer!"));
                    }

                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Exception -- MonitorForContentWorker ex - {0}", ex));
            }
            finally
            {
                this._isRunning = false;
            }
        }
        #endregion

        #region GetMusicFiles method
        private void GetMusicFiles(PortableDevice.PortableDevice device)
        { 
            
        }
        #endregion

        #region GetVideoFiles method
        private void GetVideoFiles(PortableDevice.PortableDevice device)
        { 
        
        }
        #endregion

        #region GetMusicVideoFiles method
        private void GetMusicVideoFiles(PortableDevice.PortableDevice device)
        {
            try
            {
                device.Connect();

                var folder = device.GetContents();

                if (folder != null)
                {
                    foreach (var item in folder.Files)
                    {
                        GetObject(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "GetMusicVideoFiles Exception!"));
            }
            finally
            {
                device.Disconnect();
            }
        }

        private void GetObject(PortableDevice.PortableDeviceObject obj)
        {
            if (obj is PortableDevice.PortableDeviceFolder)
                GetFolderContents((PortableDevice.PortableDeviceFolder)obj);
        }

        private void GetFolderContents(PortableDevice.PortableDeviceFolder folder)
        {
            foreach (var item in folder.Files)
            {
                if (item is PortableDevice.PortableDeviceFolder)
                {
                    GetFolderContents((PortableDevice.PortableDeviceFolder)item);
                }
                else
                {
                    string ext = item.Name.ToLower().Substring(item.Name.ToLower().LastIndexOf('.') + 1);

                    if (_musicExtList.Contains(ext))
                    {
                        _musicPlayList.Add(item as PortableDevice.PortableDeviceObject);
                        //                        _musicPlayDictionary.Add(item.Name, item as PortableDevice.PortableDeviceObject);
                    }
                    else if (_videoExtList.Contains(ext))
                    {
                        _videoPlayDictionary.Add(item.Name, item as PortableDevice.PortableDeviceObject);
                    }
                }
            }
        }
        #endregion

        #region Events Handler
        public void OnMediaFound(Object objSender, EventArgs eventArgs)
        {
            if (MediaFound != null)
                MediaFound(objSender, eventArgs);
        }

        public void OnNoMediaFound(Object objSender, EventArgs eventArgs)
        {
            if (NoMediaFound != null)
                NoMediaFound(objSender, eventArgs);
        }

        public void OnScanningMedia(Object objSender, EventArgs eventArgs)
        {
            if (ScanningMedia != null)
                ScanningMedia(objSender, eventArgs);
        }
        #endregion
    }
}
