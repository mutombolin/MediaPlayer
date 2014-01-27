using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Diagnostics;
using System.Threading;

using System.IO;
using System.Threading.Tasks;

using PortableDeviceApiLib;
using PortableDeviceTypesLib;

using _tagpropertykey = PortableDeviceApiLib._tagpropertykey;
using IPortableDeviceKeyCollection = PortableDeviceApiLib.IPortableDeviceKeyCollection;
using IPortableDeviceValues = PortableDeviceApiLib.IPortableDeviceValues;
using System.Runtime.InteropServices;

namespace PortableDevice
{
    public class MediaServer
    {
        private HttpListener _listener;
        private string _filename = string.Empty;
        private bool _isStarted = false;
        private Thread _requestThread;
        private bool _isStopping = false;
        private int _numberOfRequest;
        private System.Runtime.InteropServices.ComTypes.IStream _sourceStream;

        private ManualResetEvent resetEvent;

        public event EventHandler OnLoadingFinished;

        private PortableDevice _device;
        private PortableDeviceFile _file;

        public MediaServer()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:7896/");
            _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            resetEvent = new ManualResetEvent(false);
        }

        private void RequestThread()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var context = _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
                    context.AsyncWaitHandle.WaitOne();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("RequestThread - Ex = {0}", ex));
                }
            }
        }

        private void ListenerCallback(IAsyncResult ar)
        {
/*            lock (this)
            {
                resetEvent.Reset();
                _numberOfRequest++;
            }
*/
            while (_isStopping)
                Thread.Sleep(100);

            var listener = ar.AsyncState as HttpListener;

            System.Diagnostics.Debug.WriteLine(string.Format("ListenerCallback numberOfRequest = {0}", _numberOfRequest));

            _isStarted = true;

            var context = listener.EndGetContext(ar);

            System.Threading.Tasks.Task.Factory.StartNew((ctx) =>
            {
                WriteFile((HttpListenerContext)ctx);
            }, context, System.Threading.Tasks.TaskCreationOptions.LongRunning);
/*
            lock (this)
            {
                if (--_numberOfRequest == 0)
                    resetEvent.Set();
            }
*/
        }

        public void Stop()
        {
/*            lock (this)
            {
                _isStopping = true;
            }
*/
//            if (_isStarted)
//                resetEvent.WaitOne();

            if (_isStarted)
                _isStopping = true;
            _isStarted = false;
            System.Diagnostics.Debug.WriteLine("Listener Stopped");
        }

        public void Dispose()
        {
            while (_requestThread.IsAlive)
            {
                _listener.Stop();
                Thread.Sleep(10);
            }

            _listener.Close();
            _listener.Abort();
        }

        public void Start()
        {
            _listener.Start();
            _listener.IgnoreWriteExceptions = true;

            _requestThread = new Thread(RequestThread);
            _requestThread.Name = "RequestThread";
            _requestThread.Start();

            _numberOfRequest = 0;
            _sourceStream = null;
        }

        public bool IsStarted
        {
            get
            {
                return _isStarted;
            }
        }

        private static IntPtr ReadBuffer;

        private static int IStreamRead(System.Runtime.InteropServices.ComTypes.IStream stream, byte[] buffer)
        {
            if (ReadBuffer == IntPtr.Zero)
                ReadBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int)));

            stream.Read(buffer, buffer.Length, ReadBuffer);

            return Marshal.ReadInt32(ReadBuffer);
        }

        void WriteFile(HttpListenerContext ctx)
        {
            if ((this._device == null) || (this._file == null))
                return;

            while (_sourceStream != null)
                Thread.Sleep(50);

            try
            {
                var response = ctx.Response;

                PortableDevice device = this._device;
                PortableDeviceFile file = this._file;

                device.Connect();

                IPortableDeviceContent content;
                device.PortableDeviceClass.Content(out content);

                IPortableDeviceResources resources;
                content.Transfer(out resources);

                PortableDeviceApiLib.IStream wpdStream;
                uint optimalTransferSize = 0;

                var property = new _tagpropertykey();
                property.fmtid = new Guid(0xE81E79BE, 0x34F0, 0x41BF, 0xB5, 0x3F, 0xF1, 0xA0, 0x6A, 0xE8, 0x78, 0x42);
                property.pid = 0;

                resources.GetStream(file.Id, ref property, 0, ref optimalTransferSize, out wpdStream);
                _sourceStream = (System.Runtime.InteropServices.ComTypes.IStream)wpdStream;

                response.ContentLength64 = 0;
                response.SendChunked = true;
                response.ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
                response.AddHeader("Content-disposition", "attachment; filename=1.mp4");

                byte[] buffer = new byte[64 * 1024];
                int read;
                int Count = 0;

                using (BinaryWriter bw = new BinaryWriter(response.OutputStream))
                {
                    do
                    {
                        read = IStreamRead(_sourceStream, buffer);

                        try
                        {
                            bw.Write(buffer, 0, read);
                            bw.Flush();
                            Count += read;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                                ex,
                                "MediaServer - BinaryWriter Exception!"));
                        }

                        if (_isStopping)
                            break;
                    } while (read > 0);

                    _sourceStream.Commit(0);
                    _sourceStream = null;

                    _isStopping = false;

                    try
                    {
                        bw.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("Exception ex = {0}", ex));
                    }
                }
                System.Diagnostics.Debug.WriteLine(string.Format("Count = {0}", Count));
                OnLoadingFinished(this, new EventArgs());

                response.StatusCode = (int)HttpStatusCode.OK;
                response.StatusDescription = "OK";
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                    ex,
                    "MediaServer WriteFile Exception!"));
            }
            finally
            {
                _isStarted = false;
            }
        }

        public string FileName
        {
            set
            {
                if (value != string.Empty)
                    _filename = value;
            }
            get
            {
                return _filename;
            }
        }

        public PortableDevice Device
        {
            set
            {
                _device = value;
            }
            get
            {
                return _device;
            }
        }

        public PortableDeviceFile FileObject
        {
            set
            {
                _file = value;
            }
            get
            {
                return _file;
            }
        }
    }
}
