using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPlayer.Data
{
    public class PortableObject
    {
        private PortableDevice.PortableDevice _device;

        private Dictionary<string, PortableDevice.PortableDeviceObject> _musicPlayDictionary;
        private Dictionary<string, PortableDevice.PortableDeviceObject> _videoPlayDictionary;

        private List<string> _objectList;

        public PortableObject()
        {
            _musicPlayDictionary = new Dictionary<string, PortableDevice.PortableDeviceObject>();
            _videoPlayDictionary = new Dictionary<string, PortableDevice.PortableDeviceObject>();

            _objectList = new List<string>();
        }

        public void Dispose()
        {
            _musicPlayDictionary.Clear();
            _videoPlayDictionary.Clear();
        }

        public PortableDevice.PortableDevice Device
        {
            get
            {
                return _device;
            }
            set
            {
                _device = value;
            }
        }

        public Dictionary<string, PortableDevice.PortableDeviceObject> MusicDictionary
        {
            get
            {
                return _musicPlayDictionary;
            }
            set
            {
                _musicPlayDictionary = value;
            }
        }

        public Dictionary<string, PortableDevice.PortableDeviceObject> VideoPlayDictionary
        {
            get
            {
                return _videoPlayDictionary;
            }
            set
            {
                _videoPlayDictionary = value;
            }
        }

        public List<string> ObjectList
        {
            get
            {
                return _objectList;
            }
            set
            {
                _objectList = value;
            }
        }
    }
}
