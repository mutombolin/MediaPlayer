using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPlayer.Data
{
    public class PortableDevices
    {
        private PortableDevice.PortableDevice _device;

        private Dictionary<string, PortableDevice.PortableDeviceObject> _musicPlayDictionary;
        private Dictionary<string, PortableDevice.PortableDeviceObject> _videoPlayDictionary;

        public PortableDevices()
        {
            _musicPlayDictionary = new Dictionary<string, PortableDevice.PortableDeviceObject>();
            _videoPlayDictionary = new Dictionary<string, PortableDevice.PortableDeviceObject>();
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
    }
}
