using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    public class Client
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
        public int ClinetId { get { return clinetId; } }


        System.Net.IPAddress IPAddress = null;
        int Port = 0;
        System.Net.IPEndPoint point = null;
        System.Net.Sockets.Socket socket = null;
        bool noDelay = false;
        bool isDispose = false;
        bool isConnencted = false;
        int clinetId = 0;
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
            var IPHostEntry = System.Net.Dns.GetHostEntry(host);
            if(IPHostEntry==null|| IPHostEntry.AddressList==null|| IPHostEntry.AddressList.Length < 1)
            {
                throw new ArgumentException("uri");
            }
            foreach(var addr in IPHostEntry.AddressList)
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

            Port = uri.Port;

            point = new System.Net.IPEndPoint(IPAddress, Port);
            lastException = new Exception("Normal");
            TargetURI = uri;
        }

        void _connect(bool reConnect = false)
        {
            if (isConnencted) { return; }

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
                isConnencted = false;
                if (OnConnectErrorHandler != null)
                {
                    bool retry = false;
                    try
                    {
                        System.Net.Sockets.SocketException socketException;
                        if (ex is System.Net.Sockets.SocketException)
                        {
                            socketException = ex as System.Net.Sockets.SocketException;
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

                    if (retry)
                    {
                        System.Threading.Thread.Sleep(2000);
                        _connect(true);//重试
                    }
                    else
                    {
                        lastException = new Exception("连接失败后放弃重试");
                        Disconnect();
                        return;
                    }
                }
            }
        }
        void ReceiveData()
        {
            DataPacker myDataUnpack = new DataPacker();
            bool hasOnReceivedHandler = OnReceivedHandler != null;
            bool isFirst = true;
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
                List<IRawSocketData> lis = myDataUnpack.Decode(real);
                if (lis.Count < 1)
                {
                    continue;
                }
                if (isFirst)
                {
                    isFirst = false;
                    if (lis[0].Type != 1)
                    {
                        lastException = new Exception("Handshake failed: Type(" + lis[0].Type+")");
                        Disconnect();
                        break;
                    }
                    if (lis[0].Content.Length == 2 && lis[0].Content[0] == 79 && lis[0].Content[1] == 75)//OK
                    {
                        if (OnConnectedHandler != null)
                        {
                            try
                            {
                                OnConnectedHandler();
                            }
                            catch { }
                        }
                        lis.RemoveAt(0);
                    }
                    else
                    {
                        string msg = "";
                        if(lis[0].Type == 1)
                        {
                            msg = System.Text.Encoding.UTF8.GetString(lis[0].Content);
                        }
                        else
                        {
                            msg = "Type(" + lis[0].Type + ")";
                        }
                        lastException = new Exception("Handshake failed: " + msg);
                        Disconnect();
                        break;
                    }
                }
                if (hasOnReceivedHandler)
                {
                    foreach (IRawSocketData data in lis)
                    {
                        try
                        {
                            OnReceivedHandler(data);
                        }
                        catch { }
                    }
                }
            }
        }


        private void HttpUpgrade()
        {
            string data = "GET "+ TargetURI.PathAndQuery + " HTTP/1.1\r\n"
                + "Upgrade:rawsocket\r\n\r\n";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);
            socket.Send(buffer, buffer.Length, System.Net.Sockets.SocketFlags.None);
        }
        public bool SendData(IRawSocketData data)
        {
            if (IsConnencted)
            {
                byte[] buffer = DataPacker.Encode(data);
                socket.Send(buffer, buffer.Length, System.Net.Sockets.SocketFlags.None);
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
            if (isDispose) { return; }
            isDispose = true;
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
