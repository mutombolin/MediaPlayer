﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.IO.Pipes;

namespace MediaPlayer
{
    // Delegate for passing received message back to caller
    public delegate void DelegateMessage(string Reply);

    class PipeServer
    {
        public event DelegateMessage PipeMessage;
        public event EventHandler StatusEvent;

        string _pipeName;
        public bool _InOut = true;

        public void Listen(string PipeName)
        {
            try
            {
                // Set to class level var so we can re-use in the async callback method
                _pipeName = PipeName;

                // Create the new async pipe
                NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                // Wait for a connection
                pipeServer.BeginWaitForConnection(new AsyncCallback(WaitForConnectionCallBack), pipeServer);
            }
            catch (Exception oEX)
            {
                Debug.WriteLine(oEX.Message);
            }
        }

        private void WaitForConnectionCallBack(IAsyncResult iar)
        {
            try
            {
                // Get the pipe
                NamedPipeServerStream pipeServer = (NamedPipeServerStream)iar.AsyncState;

                // End waiting for the connection
                pipeServer.EndWaitForConnection(iar);

                byte[] buffer = new byte[255];

                // Read the incoming message
                pipeServer.Read(buffer, 0, 255);

                // Convert byte buffer to string
                string stringData = Encoding.Default.GetString(buffer).TrimEnd('\0');

                if (stringData.ToLower().Contains("read"))
                {
                    _InOut = false;
                }
                else
                {
                    _InOut = true;
                }

                if (_InOut)
                {
                    PipeMessage.Invoke(stringData);
                }
                else
                {
                    StatusEvent(this, new EventArgs());
                    byte[] recBuffer = new byte[255];

                    if (stringData.ToLower().Contains("readmediafiletype"))
                    {
                        recBuffer = System.Text.Encoding.Default.GetBytes(MediaFileType);
                    }
                    else if (stringData.ToLower().Contains("read"))
                    {
                        recBuffer = System.Text.Encoding.Default.GetBytes(StatusMessage);
                    }

                    pipeServer.Write(recBuffer, 0, recBuffer.Length);
                }

                // Kill original server and create new wait server
                pipeServer.Close();
                pipeServer = null;
                pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                // Recursively wait for the connection again and again...
                pipeServer.BeginWaitForConnection(new AsyncCallback(WaitForConnectionCallBack), pipeServer);
            }
            catch
            {
                return;
            }
        }

        public bool IsBrowserVisible { set; get; }
        public string StatusMessage { set; get; }
        public string MediaFileType { set; get; }
    }
}
