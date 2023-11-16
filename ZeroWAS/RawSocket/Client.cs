using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    public class Client : IDisposable
    {
        public class ConnectErrorEventArgs : EventArgs
        {
            private bool _Retry = false;
            public bool Retry { get { return _Retry; } set { _Retry = value; } }
            private System.Net.Sockets.SocketException _SocketException;
            public System.Net.Sockets.SocketException SocketException { get { return _SocketException; } }
            public ConnectErrorEventArgs(System.Net.Sockets.SocketException SocketException)
            {
                _SocketException = SocketException;
            }
        }
        public delegate void ConnectErrorHandler(ConnectErrorEventArgs e);
        public delegate void ConnectedHandler();
        public delegate void ReceivedHandler(IRawSocketData data);
        public delegate void DisconnectHandler(Exception ex);

        public ConnectErrorHandler OnConnectErrorHandler { get; set; }
        public ConnectedHandler OnConnectedHandler { get; set; }
        public DisconnectHandler OnDisconnectHandler { get; set; }
        public ReceivedHandler OnReceivedHandler { get; set; }
        /// <summary>
        /// 客户端标识编号(连接成功后才是有效编号)
        /// </summary>
        public long ClinetId { get { return clinetId; } }


        System.Net.IPAddress IPAddress = null;
        int Port = 0;
        System.Net.IPEndPoint point = null;
        System.Net.Sockets.Socket socket = null;
        bool noDelay = false;
        bool isDisposed = false;
        bool isConnencted = false;
        long clinetId = 0;
        System.Exception lastException = null;
        System.Uri TargetURI = null;

        /// <summary>
        /// 连接成功标识
        /// </summary>
        public bool IsConnencted { get { return isConnencted; } }

        public Client(System.Uri uri)
        {
            var host = uri.DnsSafeHost;
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("uri");
            }
            if (System.Text.RegularExpressions.Regex.IsMatch(host, @"^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$"))
            {
                IPAddress = System.Net.IPAddress.Parse(host);
            }
            else
            {
                var IPHostEntry = System.Net.Dns.GetHostEntry(host);
                if (IPHostEntry == null || IPHostEntry.AddressList == null || IPHostEntry.AddressList.Length < 1)
                {
                    throw new ArgumentException("uri");
                }
                foreach (var addr in IPHostEntry.AddressList)
                {
                    if (addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        continue;
                    }
                    IPAddress = addr;
                    break;
                }
                if (IPAddress == null)
                {
                    throw new ArgumentException("uri");
                }
            }

            Port = uri.Port;

            point = new System.Net.IPEndPoint(IPAddress, Port);
            lastException = new Exception("Normal");
            TargetURI = uri;
        }

        void _connect()
        {
        Conn:
            if (isConnencted || isDisposed) { return; }
            Exception error = null;
            try
            {
                socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                //Console.WriteLine("连接：{0}:{1}", IPAddress, Port);
                var result = socket.BeginConnect(point, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(5000, true);
                if (success)
                {
                    socket.EndConnect(result);

                    socket.NoDelay = this.noDelay;
                    isConnencted = true;
                    lastException = new Exception("Normal");

                    System.Threading.Thread receiveThread = new System.Threading.Thread(ReceiveData);
                    receiveThread.IsBackground = true;
                    receiveThread.Start();

                    HttpUpgrade();
                }
                else
                {
                    socket.Close();
                    throw new System.Net.Sockets.SocketException(10060);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            if (error != null)
            {
                isConnencted = false;
                bool retry = false;
                if (OnConnectErrorHandler != null)
                {
                    try
                    {
                        System.Net.Sockets.SocketException socketException;
                        if (error is System.Net.Sockets.SocketException)
                        {
                            socketException = error as System.Net.Sockets.SocketException;
                        }
                        else
                        {
                            //10060: 连接尝试超时，或者连接的主机没有响应。
                            socketException = new System.Net.Sockets.SocketException(10060);
                        }
                        var exception = new ConnectErrorEventArgs(socketException);
                        OnConnectErrorHandler(exception);
                        retry = exception.Retry;
                    }
                    catch { }
                }
                if (retry)
                {
                    System.Threading.Thread.Sleep(2000);
                    goto Conn;
                }
                lastException = new Exception("连接失败后放弃重试");
                Disconnect();
            }
        }
        bool isFirst = true;
        bool hasOnReceivedHandler = false;
        void ReceiveData()
        {
            IDataFrameReceiver receiver = new DataFrameReceiver();
            receiver.OnReceived += Receiver_OnReceived;
            hasOnReceivedHandler = OnReceivedHandler != null;
            isFirst = true;
            int len = 2048;
            while (isConnencted)
            {

                byte[] buffer = new byte[len];
                int bound = 0;
                try
                {
                    bound = socket.Receive(buffer);
                    if (bound < 1)
                    {
                        throw new Exception("0字节");
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Disconnect();
                    break;
                }
                byte[] real;
                if (bound < len)
                {
                    real = new byte[bound];
                    Array.Copy(buffer, real, bound);
                }
                else
                {
                    real = buffer;
                }
                try
                {
                    receiver.Receive(real);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Disconnect();
                    break;
                }
            }
        }
        private void Receiver_OnReceived(IRawSocketData frame)
        {
            if (isFirst)
            {
                isFirst = false;
                if (OnConnectedHandler != null)
                {
                    try
                    {
                        OnConnectedHandler();
                    }
                    catch { }
                }
            }
            if (hasOnReceivedHandler)
            {
                try
                {
                    OnReceivedHandler(frame);
                }
                catch { }
            }
            frame.Dispose();
        }

        private void HttpUpgrade()
        {
            string data = "GET " + TargetURI.PathAndQuery + " HTTP/1.1\r\n"
                + "Upgrade:rawsocket\r\n\r\n";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);
            socket.Send(buffer, buffer.Length, System.Net.Sockets.SocketFlags.None);
        }
        public bool SendData(IRawSocketData data)
        {
            if (IsConnencted)
            {
                data.ReadAll(e => {
                    socket.Send(e.Data, e.Data.Length, System.Net.Sockets.SocketFlags.None);
                    if (e.IsEnd)
                    {
                        data.Dispose();
                    }
                });
                return true;
            }
            return false;
        }
        public void Connect()
        {
            _connect();
        }
        public void Disconnect()
        {
            if (!isConnencted) { return; }
            isConnencted = false;
            clinetId = 0;
            socket.Close();
            if (OnDisconnectHandler != null)
            {
                try
                {
                    OnDisconnectHandler(lastException);
                }
                catch { }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        protected void Dispose(bool dispoing)
        {
            if (isDisposed) { return; }
            isDisposed = true;
            isConnencted = false;
            clinetId = 0;
            if (dispoing)
            {
                try
                {
                    if (socket != null)
                    {
                        socket.Close();
                    }
                }
                catch { }
            }
        }



    }
}
