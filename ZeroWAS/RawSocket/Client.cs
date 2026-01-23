using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ZeroWAS.RawSocket
{
    public sealed class Client : IDisposable
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
        public delegate void ReceivedHandler(IRawSocketReceivedMessage data);
        public delegate void DisconnectHandler(Exception ex);

        public ConnectErrorHandler OnConnectErrorHandler { get; set; }
        public ConnectedHandler OnConnectedHandler { get; set; }
        public DisconnectHandler OnDisconnectHandler { get; set; }
        public ReceivedHandler OnReceivedHandler { get; set; }
        /// <summary>
        /// 客户端标识编号(连接成功后才是有效编号)
        /// </summary>
        public long ClientId { get { return clientId; } }


        System.Net.IPAddress IPAddress = null;
        int Port = 0;
        System.Net.IPEndPoint point = null;
        System.Net.Sockets.Socket socket = null;
        bool noDelay = false;
        /// <summary>
        /// 释放状态：0 running 1 disposed
        /// </summary>
        int _disposed = 0;
        bool isConnencted = false;
        long clientId = 0;
        System.Exception lastException = null;
        readonly System.Uri TargetURI = null;
        Dictionary<byte, ReceivedHandler> receivedHandlers = new Dictionary<byte, ReceivedHandler>();
        private readonly object _handlerLock = new object();

        /// <summary>
        /// 连接成功标识
        /// </summary>
        public bool IsConnected { get { return isConnencted; } }

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
            if (isConnencted || _disposed == 1) { return; }
            Exception error = null;
            try
            {
                socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
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
        int isFirst = 1;
        private void ReceiveData()
        {
            MessageReceiver receiver = new MessageReceiver();
            receiver.OnMessage += Receiver_OnMessage;
            int len = 4096;
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
                    receiver.Receive(buffer, 0, bound);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Disconnect();
                    break;
                }
            }
        }
        private void Receiver_OnMessage(ReceivedMessage obj)
        {
            try
            {
                // 首包逻辑
                if (Interlocked.Exchange(ref isFirst, 0) == 1)
                {
                    if (obj.Type == 1)
                    {
                        string s = obj.ReadContentAsString(Encoding.UTF8);
                        if (s.StartsWith("ClientId="))
                        {
                            s = s.Substring(8);
                            if (s.Length > 0 && !long.TryParse(obj.Remark, out clientId))
                            {
                                clientId = -1;
                            }
                        }
                    }

                    try
                    {
                        OnConnectedHandler?.Invoke();
                    }
                    catch { }
                }

                // 专用 handler
                if (TryGetReceivedHandler(obj.Type, out var handler))
                {
                    try
                    {
                        handler(obj);
                    }
                    catch { }
                    return;
                }

                // 通用 handler
                try
                {
                    OnReceivedHandler?.Invoke(obj);
                }
                catch { }
            }
            finally
            {
                obj.Dispose();
            }
        }
        private void HttpUpgrade()
        {
            string data = "GET " + TargetURI.PathAndQuery + " HTTP/1.1\r\n"
                + "Host:" + TargetURI.Authority + "\r\n"
                + "Upgrade:rawsocket\r\n\r\n";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);
            socket.Send(buffer, buffer.Length, System.Net.Sockets.SocketFlags.None);
        }

        private object _sendLock = new object();
        public bool SendData(IRawSocketSendMessage data, bool autoDispose = true)
        {
            bool reval = false;
            if (IsConnected)
            {
                SerializedMessage serializedMessage = new SerializedMessage(data);
                try
                {
                    serializedMessage.Take();
                    serializedMessage.EndDispatch();
                    lock (_sendLock)
                    {
                        serializedMessage.Read((bytes) =>
                        {
                            socket.Send(bytes, bytes.Length, System.Net.Sockets.SocketFlags.None);
                        });
                    }
                }
                finally {
                    serializedMessage.End();
                }
                reval = true;
            }
            if (autoDispose) { data.Dispose(); }
            return reval;
        }
        public void Connect()
        {
            _connect();
        }
        public void Disconnect()
        {
            if (!isConnencted) { return; }
            isConnencted = false;
            clientId = 0;
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
            if (Interlocked.Exchange(ref _disposed, 1) != 0) { return; }
            isConnencted = false;
            clientId = 0;
            if (socket != null)
            {
                try
                {
                    socket.Close();
                    socket = null;
                }
                catch { }
            }
        }

        public bool ReceivedHandleRegister(byte type, ReceivedHandler handler, bool overwrite = true)
        {
            if (handler == null) { return false; }
            lock (_handlerLock)
            {
                if (!overwrite && receivedHandlers.TryGetValue(type, out _))
                {
                    return false;
                }
                receivedHandlers[type] = handler;
                return true;
            }
        }
        public bool ReceivedHandleRemove(byte type)
        {
            lock (_handlerLock)
            {
                return receivedHandlers.Remove(type);
            }
        }
        private bool TryGetReceivedHandler(byte type, out ReceivedHandler handler)
        {
            lock (_handlerLock)
            {
                return receivedHandlers.TryGetValue((byte)type, out handler);
            }
        }

    }
}
