using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

using PortableDevice;

namespace MediaPlayer.Common
{
    public class PortableDeviceHelper
    {
        private List<PortableDevice.PortableDevice> _portableList = new List<PortableDevice.PortableDevice>();
        private List<PortableDeviceObject> _musicObjectList = new List<PortableDeviceObject>();
        private List<PortableDeviceObject> _videoObjectList = new List<PortableDeviceObject>();

        private int _currentDetectedDevices = 0;
        private bool _stopThread = false;
        private Thread _driveSearchThread = null;

        public event EventHandler LoadedCompleted;

        private event EventHandler MediaFound;

        public static readonly PortableDeviceHelper Instance = new PortableDeviceHelper();

        public PortableDeviceHelper()
        {
            this._stopThread = false;
            this._driveSearchThread = new Thread(new ThreadStart(MonitorForContentWorker));
            this._driveSearchThread.Name = "PortableDeviceHelper.MonitorForContentWorker";
            this._driveSearchThread.Start();
        }

        public void LoadPortableDevice()
        {
            
        }

        public void Stop()
        {
            this._stopThread = true;
        }

        #region Monitorforcontentworker thread
        private void MonitorForContentWorker()
        {
            try
            {
                while (!this._stopThread)
                {
                    int deviceCount = 0;
                    var devices = new PortableDevice.PortableDeviceCollection();
                    devices.Refresh();

                    deviceCount = devices.Count;

                    if (deviceCount != this._currentDetectedDevices)
                    {
                        Clear();

                        foreach (PortableDevice.PortableDevice device in devices)
                        {
                            device.Connect();
                            _portableList.Add(device);
                            LoadContents(device);
                            device.Disconnect();
                        }

                        OnLoadedCompleted();

                        _currentDetectedDevices = deviceCount;
                    }

                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "PortableDeviceHelper.MonitorForContentWorker exception inner!"));
            }
            finally
            { 
                
            }
        }
        #endregion

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
                    if (item.Name.ToLower().Contains("mp3") ||
                        item.Name.ToLower().Contains("wav") ||
                        item.Name.ToLower().Contains("aiff"))
                    {
                        _musicObjectList.Add(item as PortableDevice.PortableDeviceObject);
                    }

                    if (item.Name.ToLower().Contains("mpg") ||
                        item.Name.ToLower().Contains("mp4") ||
                        item.Name.ToLower().Contains("mov") ||
                        item.Name.ToLower().Contains("wmv") ||
                        item.Name.ToLower().Contains("avi"))
                    {
                        _videoObjectList.Add(item as PortableDevice.PortableDeviceObject);
                    }
                }
            }
        }

        private void GetObject(PortableDevice.PortableDeviceObject obj)
        {
            if (obj is PortableDevice.PortableDeviceFolder)
                GetFolderContents((PortableDevice.PortableDeviceFolder)obj);
        }

        private void LoadContents(PortableDevice.PortableDevice device)
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

        private void Clear()
        {
            if (_portableList.Count > 0)
                _portableList.Clear();

            if (_musicObjectList.Count > 0)
                _musicObjectList.Clear();

            if (_videoObjectList.Count > 0)
                _videoObjectList.Clear();
        }

        private void OnLoadedCompleted()
        { 
            if (LoadedCompleted != null)
                LoadedCompleted(this, new EventArgs());
        }

        #region Properties
        public List<string> MusicList
        {
            get
            {
                List<string> tmpList = new List<string>();

                foreach (PortableDevice.PortableDeviceObject obj in _musicObjectList)
                {
                    tmpList.Add(obj.Name.ToString());
                }

                return tmpList;
            }
        }

        public List<string> VideoList
        {
            get
            {
                List<string> tmpList = new List<string>();

                foreach (PortableDevice.PortableDeviceObject obj in _videoObjectList)
                    tmpList.Add(obj.Name.ToString());

                return tmpList;
            }
        }

        public List<string> DeviceList
        {
            get
            {
                List<string> tmpList = new List<string>();

                foreach (PortableDevice.PortableDevice device in _portableList)
                {
                    device.Connect();
                    tmpList.Add(device.FriendlyName);
                    device.Disconnect();
                }

                return tmpList;
            }
        }
        #endregion
    }
}
