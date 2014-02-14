using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Threading;

using MediaPlayer.Data;

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
        private List<PortableDevice.PortableDevice> _portableList = new List<PortableDevice.PortableDevice>();
        private List<PortableObject> _portableObjectList = new List<PortableObject>();
        private List<string> _musicExtList = new List<string>();
        private List<string> _videoExtList = new List<string>();

        private List<string> _folderHighPriorityList = new List<string>();

        private List<PortableDevice.PortableDeviceObject> _musicPlayList = new List<PortableDevice.PortableDeviceObject>();
        private List<PortableDevice.PortableDeviceObject> _videoPlayList = new List<PortableDevice.PortableDeviceObject>();

        private List<string> _filterList = new List<string>();

        private PortableObject _currentPortableObj = null;

        private bool _anyReady = false;
        private bool _isSearchingMusic = false;
        private bool _isSearchingVideo = false;
        private bool _isSearching = false;
        private bool _pauseSearching = false;
        private bool _isPaused = false;
        private bool _isPausing = false;

        private bool _isHierarchyMode = false;

        private System.Timers.Timer _deviceSearchTimer;

        private System.Timers.Timer _contentUpdateTimer;

        #region Events
        public event EventHandler MediaFound;
        public event EventHandler NoMediaFound;
        public event EventHandler ScanningMedia;
        public event EventHandler UpdateMedia;
        #endregion

        public static readonly MediaContentManager Instance = new MediaContentManager();

        private MediaContentManager()
        {
            this._deviceSearchTimer = new System.Timers.Timer();
            this._deviceSearchTimer.Interval = 3000;
            this._deviceSearchTimer.Elapsed += new System.Timers.ElapsedEventHandler(_deviceSearchTimer_Elapsed);

            this._contentUpdateTimer = new System.Timers.Timer();
            this._contentUpdateTimer.Interval = 3000;
            this._contentUpdateTimer.Elapsed += new System.Timers.ElapsedEventHandler(_contentUpdateTimer_Elapsed);
        }

        static MediaContentManager()
        {
        }

        #region Properties
        public List<PortableDevice.PortableDeviceObject> MusicPlayList
        {
            get { return this._musicPlayList; }
        }

        public List<PortableDevice.PortableDeviceObject> VideoPlayList
        {
            get { return this._videoPlayList; }
        }

        public List<PortableObject> PortableObjectList
        {
            get { return this._portableObjectList; }
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

        public bool IsSearching
        {
            get { return this._isSearching; }
        }

        public bool IsPaused
        {
            get 
            {
                if (_isPausing)
                    return false;
                else
                    return this._isPaused; 
            }
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

        public bool IsHierarchyMode
        {
            get { return _isHierarchyMode; }
            set { _isHierarchyMode = value;}
        }
        #endregion

        #region Init method
        public void Init()
        {
            this._devicesDetected = 0;
            this._portableList.Clear();
            this._portableObjectList.Clear();
            this._musicPlayList.Clear();
            this._videoPlayList.Clear();
            this._musicExtList.Clear();
            this._videoExtList.Clear();
            this._folderHighPriorityList.Clear();

            this._musicExtList.Add("mp3");
            this._musicExtList.Add("wav");
            this._musicExtList.Add("aiff");
            this._musicExtList.Add("wma");

            this._videoExtList.Add("mpg");
            this._videoExtList.Add("mp4");
            this._videoExtList.Add("mov");
            this._videoExtList.Add("wmv");
            this._videoExtList.Add("avi");

            this._folderHighPriorityList.Add("music");
            this._folderHighPriorityList.Add("musics");
            this._folderHighPriorityList.Add("video");
            this._folderHighPriorityList.Add("videos");

            OnUpdateMedia(this, new EventArgs());
            this.StartSearch();
        }
        #endregion

        #region Device Insert/Remove
        public void StartSearch()
        {
            this._deviceSearchTimer.Stop();
            this._deviceSearchTimer.Start();
        }

        public void PauseSearch()
        {
            if (_isSearching)
            {
                _pauseSearching = true;
                _isPausing = true;
            }
        }

        public void ResumeSearch()
        {
            if (_isSearching && _pauseSearching)
            {
                _pauseSearching = false;
            }
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

                int retry = 0;

                while (!this._stopThread)
                {
                    try
                    {
                        this._anyReady = false;
                        int deviceCount = 0;
                        var devices = new PortableDevice.PortableDeviceCollection();
                        devices.Refresh();

                        deviceCount = devices.Count;

                        System.Diagnostics.Debug.WriteLine(string.Format("device Count = {0}", deviceCount));

                        if (deviceCount != this._devicesDetected)
                        {
                            if (deviceCount > 0)
                            {
                                foreach (PortableDevice.PortableDevice device in devices)
                                {
                                    device.Connect();
                                    if (!this._filterList.Contains(device.FriendlyName))
                                    {
                                        PortableObject portableObj = new PortableObject();
                                        portableObj.Device = device;
                                        _portableObjectList.Add(portableObj);
                                        _portableList.Add(device);
                                        //                                    device.Disconnect();
                                    }
                                }

                                this._devicesDetected = deviceCount;
                            }

                            if (_portableList.Count > 0)
                            {
                                OnScanningMedia(this, new EventArgs());

                                this._anyReady = true;

                                foreach (PortableObject deviceObj in _portableObjectList)
                                {
                                    GetMusicVideoFiles(deviceObj);
                                }
                            }

                            if ((this._musicPlayList.Count > 0) || (this._videoPlayList.Count > 0))
                                OnMediaFound(this, new EventArgs());
                            else
                                OnNoMediaFound(this, new EventArgs());

                            if (!this._anyReady)
                            {
                                this._portableList.Clear();
                                this._portableObjectList.Clear();
                                this._musicPlayList.Clear();
                                this._videoPlayList.Clear();

                                OnNoMediaFound(this, new EventArgs());
                            }
                            this._stopThread = true;
                        }
                        else
                        {
                            retry++;
                        }
                    }
                    catch (Exception ex)
                    { 
                        System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                            System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                            ex,
                            "MediaContentManager.MonitorForContentWorker Exception outer!"));
                    }

                    System.Diagnostics.Debug.WriteLine(string.Format("retry times = {0}", retry));

                    if (retry > 4)
                        this._stopThread = true;

                    if (!this._stopThread)
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

        #region Device Search Timer
        private void _deviceSearchTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this._deviceSearchTimer.Stop();

            while ((this._deviceSearchThread != null) && (this._deviceSearchThread.IsAlive))
            {
                this._stopThread = true;
                Thread.Sleep(1000);
            }
            this._stopThread = false;
            this._deviceSearchThread = new Thread(new ThreadStart(MonitorForContentWorker));
            this._deviceSearchThread.Name = "MediaContentManager.MonitorForContent";
            this._deviceSearchThread.Start();
        }
        #endregion

        #region Content update timer
        void _contentUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {

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
        private void GetMusicVideoFiles(PortableObject deviceObj)
        {
            this._isSearching = true;

            try
            {
                PortableDevice.PortableDevice device = deviceObj.Device;
                _currentPortableObj = deviceObj;

                device.Connect();

                Dictionary<PortableDevice.PortableDeviceFolder, string> dicFolders = new Dictionary<PortableDevice.PortableDeviceFolder, string>();
                Dictionary<PortableDevice.PortableDeviceFolder, string> dicPriorityFolders = new Dictionary<PortableDevice.PortableDeviceFolder, string>();

                PortableDevice.PortableDeviceFolder folder;

                bool searchFirst = false;
                bool isPrioritySearch = false;

                bool update = false;
                int currentMusicCount = 0;
                int currentVideoCount = 0;

                string topPath = string.Empty;
                string currentPath = string.Empty;
                string tmpPath = string.Empty;

                do
                {
                    while (_pauseSearching)
                    {
                        _isPausing = false;

                        _isPaused = true;
                        Thread.Sleep(5000);
                    }

                    _isPaused = false;

                    if (dicFolders.Count == 0)
                    {
                        folder = device.GetContents(new PortableDevice.PortableDeviceFolder("DEVICE", "DEVICE"));
                    }
                    else
                    {
                        PortableDevice.PortableDeviceFolder root;

                        string name = string.Empty;

                        if (dicPriorityFolders.Count > 0)
                        {
                            isPrioritySearch = true;

                            root = dicPriorityFolders.First().Key;
                            name = dicPriorityFolders.First().Value;
/*
                            var root = dicPriorityFolders.First();

                            folder = device.GetContents(new PortableDevice.PortableDeviceFolder(root.Key.Id, root.Key.Name));

                            if (currentPath == string.Empty)
                                currentPath = folder.Name;
                            else
                                currentPath = root.Value;
                            dicPriorityFolders.Remove(root.Key);
*/
                        }
                        else
                        {
                            isPrioritySearch = false;

                            root = dicFolders.First().Key;
                            name = dicFolders.First().Value;
/*
                            var root = dicFolders.First();
                            try
                            {
                                folder = device.GetContents(new PortableDevice.PortableDeviceFolder(root.Key.Id, root.Key.Name));
                            }
                            catch { continue; }

                            if (currentPath == string.Empty)
                                currentPath = folder.Name;
                            else
                                currentPath = root.Value;

                            dicFolders.Remove(root.Key);
*/
                        }

                        try
                        {
                            folder = device.GetContents(new PortableDevice.PortableDeviceFolder(root.Id, root.Name));
                        }
                        catch { _pauseSearching = true; continue; }

                        if (currentPath == string.Empty)
                            currentPath = folder.Name;
                        else
                            currentPath = name;

                        if (isPrioritySearch)
                            dicPriorityFolders.Remove(root);
                        else
                            dicFolders.Remove(root);
                    }

                    foreach (var item in folder.Files)
                    {
                        if (item is PortableDevice.PortableDeviceFolder)
                        {
                            if (item.Name == string.Empty)
                                continue;

                            string folderLowCase = item.Name.ToLower();

                            if (_folderHighPriorityList.Contains(folderLowCase))
                                searchFirst = true;

                            if (searchFirst || isPrioritySearch)
                            {
                                dicPriorityFolders.Add(item as PortableDevice.PortableDeviceFolder, currentPath + "\\" + item.Name);        
                                searchFirst = false;
                            }
                            else
                            {
                                dicFolders.Add(item as PortableDevice.PortableDeviceFolder, currentPath + "\\" + item.Name);
                            }
                        }
                        else
                        {
                            PortableDevice.PortableDeviceFile fileItem = item as PortableDevice.PortableDeviceFile;

                            string ext = fileItem.Name.ToLower().Substring(item.Name.ToLower().LastIndexOf('.') + 1);
                            fileItem.Path = currentPath + "\\" + fileItem.Name;

                            _currentPortableObj.ObjectList.Add(fileItem.Id);

                            if (_musicExtList.Contains(ext))
                            {
                                _musicPlayList.Add(fileItem as PortableDevice.PortableDeviceObject);
                            }
                            else if (_videoExtList.Contains(ext))
                            {
                                _videoPlayList.Add(fileItem as PortableDevice.PortableDeviceObject);
                            }
                        }
                    }

                    if ((this._musicPlayList.Count != currentMusicCount) || (this._videoPlayList.Count != currentVideoCount))
                        update = true;

                    if (update)
                        OnMediaFound(this, new EventArgs());

                    update = false;
                    currentMusicCount = this._musicPlayList.Count;
                    currentVideoCount = this._videoPlayList.Count;
                } while (dicFolders.Count > 0);
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
            }
            this._isSearching = false;
        }

        private void GetObject(PortableDevice.PortableDeviceObject obj)
        {
            if (obj is PortableDevice.PortableDeviceFolder)
                GetFolderContents((PortableDevice.PortableDeviceFolder)obj, string.Empty, 0);
        }

        private void GetFolderContents(PortableDevice.PortableDeviceFolder folder, string path, int hierarchy)
        {
            string currentPath = string.Empty;

            hierarchy++;

            if (path == string.Empty)
                currentPath = folder.Name;
            else
                currentPath = path + "\\" + folder.Name;

            System.Diagnostics.Debug.WriteLine(string.Format("hierarchy = {0}", hierarchy));

            foreach (var item in folder.Files)
            {
                if (item != null)
                {
                    if (item is PortableDevice.PortableDeviceFolder)
                    {
                        GetFolderContents((PortableDevice.PortableDeviceFolder)item, currentPath, hierarchy);
                    }
                    else
                    {
                        PortableDevice.PortableDeviceFile fileItem = item as PortableDevice.PortableDeviceFile;

                        string ext = fileItem.Name.ToLower().Substring(item.Name.ToLower().LastIndexOf('.') + 1);
                        fileItem.Path = currentPath + "\\" + fileItem.Name;

                        _currentPortableObj.ObjectList.Add(fileItem.Id);

                        if (_musicExtList.Contains(ext))
                        {
                            _musicPlayList.Add(fileItem as PortableDevice.PortableDeviceObject);
                        }
                        else if (_videoExtList.Contains(ext))
                        {
                            _videoPlayList.Add(fileItem as PortableDevice.PortableDeviceObject);
                        }
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

        public void OnUpdateMedia(Object objSender, EventArgs eventArgs)
        {
            if (UpdateMedia != null)
                UpdateMedia(objSender, eventArgs);
        }
        #endregion
    }
}
