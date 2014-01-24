using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MediaPlayer.Server
{
    public class CompactRequest
    {
        public string _method, _url, _protocol;
        public Dictionary<string, string> _headers;
        public long _startIndex = 0;

        public CompactRequest(StreamReader sr)
        {
            string firstLine = sr.ReadLine();
            if (!string.IsNullOrEmpty(firstLine))
            {
                string[] p = firstLine.Split(' ');
                _method = p[0];
                _url = (p.Length > 1) ? p[1] : "NA";
                _protocol = (p.Length > 2) ? p[2] : "NA";
            }
            string line = null;
            _headers = new Dictionary<string, string>();

            while (!string.IsNullOrEmpty(line = sr.ReadLine()))
            {
                int pos = line.IndexOf(":");
                if (pos > -1)
                    _headers.Add(line.Substring(0, pos), line.Substring(pos + 1));
            }

            foreach (string key in _headers.Keys)
            {
                Console.WriteLine(string.Format("key : {0} value = {1}", key, _headers[key]));
                if (string.Compare(key, "Range", true) == 0)
                {
                    string value = _headers[key];
                    int equal = value.IndexOf("=");
                    string tmp1 = value.Substring(equal + 1);
                    int dash = tmp1.IndexOf("-");
                    string startBytes = tmp1.Substring(0, dash);
                    this._startIndex = Convert.ToInt32(startBytes);
                    Console.WriteLine(string.Format("StartIndex = {0}", this._startIndex));
                }
            }
        }
    }

    public class CompactResponse
    {
        public class HttpStatus
        {
            public static string HTTP200 = "200 OK";
            public static string HTTP404 = "404 Not Found";
            public static string HTTP500 = "500 Error";
        }

        public string StatusText = HttpStatus.HTTP200;
        public string ContentType = "text/plain";

        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        public MemoryStream DataMS;
    }

    public class HttpServer
    {
        private Thread _serverThread;
        TcpListener _listener;
        private long _ticks = 0;
        private bool _isListening = false;

        public HttpServer(int port, Func<CompactRequest, CompactResponse> reqProc)
        {
            IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
            _listener = new TcpListener(ipAddr, port);

            _serverThread = new Thread(() =>
            {
                this._listener.Start();
                this._isListening = true;
                while (_isListening)
                {
                    if (_listener.Pending())
                    {
                        Socket s = _listener.AcceptSocket();

                        if (s != null)
                        {
                            System.Net.Sockets.NetworkStream ns = new System.Net.Sockets.NetworkStream(s);

                            StreamReader sr = new StreamReader(ns);
                            CompactRequest req = new CompactRequest(sr);

                            CompactResponse resp = reqProc(req);

                            StreamWriter sw = new StreamWriter(ns);
                            sw.WriteLine("HTTP/1.1 {0}", resp.StatusText);
                            sw.WriteLine("Content-Type: " + resp.ContentType);
                            sw.WriteLine("Accept-Ranges: bytes");
                            foreach (string k in resp.Headers.Keys)
                            {
                                sw.WriteLine("{0}: {1}", k, resp.Headers[k]);
                            }

                            long length = resp.DataMS.Length - req._startIndex;

                            sw.WriteLine("Content-Length: {0}", length);
                            sw.WriteLine();
                            sw.Flush();

                            System.Diagnostics.Debug.WriteLine("HttpServer -- StreamWriter flush");

                            int read = 0;

                            resp.DataMS.Position = req._startIndex;

                            byte[] buffer = new byte[8192];

                            do
                            {
                                read = resp.DataMS.Read(buffer, 0, buffer.Length);

                                if (s.Connected == false)
                                {
                                    read = 0;
                                }
                                else
                                {
                                    try
                                    {
                                        s.Send(buffer);
                                    }
                                    catch { }
                                }

                            } while (read > 0);

                            s.Shutdown(SocketShutdown.Both);
                            ns.Close();

                            System.Diagnostics.Debug.WriteLine("HttpServer -- ns close");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("HttpServer -- No Pending request. Waiting for 500 ms");
                        Thread.Sleep(500);
                    }
                }
            });

            _serverThread.Start();
        }

        public void Stop()
        {
            while (_serverThread.IsAlive)
            {
                this._isListening = false;
                Thread.Sleep(100);
            }
            _listener.Stop();

//            _serverThread.Abort();
        }

        private void ShowTicks(bool begin)
        {
            long ticksElapsed = 0;

            if (begin)
                _ticks = DateTime.Now.Ticks;
            else
                ticksElapsed = DateTime.Now.Ticks - _ticks;

            System.Diagnostics.Debug.WriteLine(string.Format("elapsed ticks = {0}", ticksElapsed));
        }
    }
}
