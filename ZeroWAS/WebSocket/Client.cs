using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace ZeroWAS.WebSocket
{
    public class Client: IDisposable
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
        public delegate void ReceivedHandler(IWebSocketDataFrame data);
        public delegate void DisconnectHandler(Exception ex);

        public ConnectErrorHandler OnConnectErrorHandler { get; set; }
        public ConnectedHandler OnConnectedHandler { get; set; }
        public DisconnectHandler OnDisconnectHandler { get; set; }
        public ReceivedHandler OnReceivedHandler { get; set; }
        /// <summary>
        /// 客户端标识编号(连接成功后才是有效编号)
        /// </summary>
        public long ClinetId { get { return clinetId; } }
        private string _SecWebSocketVersion = "13";
        private string _SecWebSocketKey = "";
        public string SecWebSocketVersion { get { return _SecWebSocketVersion; } }
        public string SecWebSocketKey { get { return _SecWebSocketKey; } }


        System.Net.IPAddress IPAddress = null;
        int Port = 0;
        System.Net.IPEndPoint point = null;
        System.Net.Sockets.Socket socket = null;

        System.Net.Security.SslStream sslStream = null;
        System.Net.Sockets.NetworkStream ns = null;
        bool useSSL = false;

        bool noDelay = false;
        bool isDispose = false;
        bool isConnencted = false;
        long clinetId = 0;
        System.Exception lastException = null;
        System.Uri TargetURI = null;

        /// <summary>
        /// 连接成功标识
        /// </summary>
        public bool IsConnencted { get { return isConnencted; } }


        public static bool ValidateServerCertificate(
              object sender,
              System.Security.Cryptography.X509Certificates.X509Certificate certificate,
              System.Security.Cryptography.X509Certificates.X509Chain chain,
              System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }
            return false;
        }

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
            if (uri.Scheme != "ws" && uri.Scheme != "wss")
            {
                throw new ArgumentException("uri");
            }
            useSSL = uri.Scheme == "wss";

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
            bool isHandshaking = true;
            byte[] overBytes = null;
            int len = 2048;
            var dataReceiver = new DataReceiver(new DataReceiver.DataFrameCallback(DataFrameReceived), 1024 * 1024 * 4);
            while (isConnencted)
            {

                byte[] buffer = new byte[len];
                int bound = 0;
                try
                {
                    bound = socket.Receive(buffer);

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
                    if (isHandshaking)//握手阶段
                    {
                        if (Handshaking(real, out overBytes))
                        {
                            isHandshaking = false;
                            continue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else//握手完成
                    {
                        if (overBytes != null && overBytes.Length > 0)
                        {
                            dataReceiver.Received(overBytes);
                            overBytes = null;
                        }
                        dataReceiver.Received(real);
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Disconnect();
                    break;
                }
            }
        }
        bool isFrist = true;
        private void DataFrameReceived(ReceivedResult result)
        {
            if (isFrist)
            {
                isFrist = false;
                bool flag = false;
                if (result.Data != null && result.Data.Header.OpCode == 1)
                {
                    string s = result.Data.Text;
                    if (s.StartsWith("CLINETID=") && s.Length > 9)//OK
                    {
                        flag = long.TryParse(s.Substring(9), out this.clinetId);
                    }
                }
                if (OnConnectedHandler != null)
                {
                    try
                    {
                        OnConnectedHandler();
                    }
                    catch { }
                }
                if (flag)
                {
                    return;
                }
            }
            if (result.Data != null)
            {
                if (result.Data.Header.OpCode == 8)
                {
                    this.Disconnect();
                    return;
                }
                if (result.Data.Header.OpCode == 9)
                {
                    this.SendData(new DataFrame(ControlOpcodeEnum.Pong));
                    return;
                }
                if (OnReceivedHandler != null)
                {
                    try
                    {
                        OnReceivedHandler(result.Data);
                    }
                    catch { }
                }
            }
            else
            {
                lastException = new Exception(result.ErrorMessage);
                this.Disconnect();
            }
        }

        List<byte> handshakeBytes = new List<byte>();
        readonly byte[] httpResEndBytes = new byte[] { 13, 10, 13, 10 };
        private bool Handshaking(byte[] bytes, out byte[] overBytes)
        {
            bool reval = false;
            overBytes = null;
            handshakeBytes.AddRange(bytes);
            byte[] myBytes = handshakeBytes.ToArray();
            if (myBytes.Length > 1024 * 1024)
            {
                throw new Exception("握手失败(报文超长)");
            }
            int index = ByteArrayIndexOf(myBytes, httpResEndBytes, 0);
            if (index < 0)
            {
                return reval;
            }
            
            int endIndex = index + httpResEndBytes.Length;
            int dif = myBytes.Length - endIndex;
            if(dif>0)
            {
                overBytes = new byte[dif];
                Array.Copy(myBytes, endIndex, overBytes, 0, overBytes.Length);
                handshakeBytes.Clear();
                handshakeBytes = null;
            }
            string header = System.Text.Encoding.UTF8.GetString(myBytes, 0, index);
            string[] lines = header.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (!lines[0].StartsWith("HTTP/"))
            {
                throw new Exception("握手失败(报文非HTTP响应)");
            }
            string[] cols= lines[0].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (cols.Length != 4)
            {
                throw new Exception("握手失败(HttpTopLine=" + lines[0] + ")");
            }
            if (cols[1] != "101")
            {
                throw new Exception("握手失败(HttpCode=" + cols[1] + ")");
            }
            var nvc = new System.Collections.Specialized.NameValueCollection();
            for(int i = 1; i < lines.Length; i++)
            {
                string[] t= lines[i].Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                if (t.Length != 2) { continue; }
                nvc.Add(t[0].Trim(), t[1].Trim());
            }
            string upgrade = nvc["Upgrade"];
            if (!upgrade.Equals("WebSocket"))
            {
                throw new Exception("握手失败(Upgrade=" + upgrade + ")");
            }
            string connection = nvc["Connection"];
            if (!connection.Equals("Upgrade"))
            {
                throw new Exception("握手失败(Connection=" + connection + ")");
            }
            string secWebSocketAccept = nvc["Sec-WebSocket-Accept"];
            string real = ComputeWebSocketHandshakeSecurityHash09(this.SecWebSocketKey);
            if (secWebSocketAccept != real)
            {
                throw new Exception("握手失败(Sec-WebSocket-Accept=" + secWebSocketAccept + ")");
            }
            return true;
        }
        private int ByteArrayIndexOf(byte[] source, byte[] frame, int sourceIndex)
        {
            if (source != null &&
                frame != null &&
                source.Length > 0 &&
                frame.Length > 0 &&
                sourceIndex > -1 &&
                source.Length >= frame.Length &&
                frame.Length + sourceIndex < source.Length)
            {
                for (int i = sourceIndex; i < source.Length - frame.Length + 1; i++)
                {
                    if (source[i] == frame[0])
                    {
                        if (frame.Length < 2) { return i; }
                        bool flag = true;
                        for (int j = 1; j < frame.Length; j++)
                        {
                            if (source[i + j] != frame[j])
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (flag) { return i; }
                    }
                }
            }
            return -1;
        }
        private string ComputeWebSocketHandshakeSecurityHash09(string secWebSocketKey)
        {
            const string MagicKEY = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string secWebSocketAccept = String.Empty;
            string ret = secWebSocketKey + MagicKEY;
            SHA1 sha = SHA1.Create();
            byte[] sha1Hash = sha.ComputeHash(Encoding.UTF8.GetBytes(ret));
            secWebSocketAccept = Convert.ToBase64String(sha1Hash);
            return secWebSocketAccept;
        }
        private void HttpUpgrade()
        {
            byte[] keys = new byte[16];
            Random random = new Random();
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = Convert.ToByte(random.Next(0, 255));
            }
            this._SecWebSocketKey = Convert.ToBase64String(keys);
            string data = "GET " + TargetURI.PathAndQuery + " HTTP/1.1\r\n"
                    + "Upgrade:websocket\r\n"
                    + "Connection:Upgrade\r\n"
                    + "Host:" + TargetURI.Authority + "\r\n"
                    + "Sec-WebSocket-Key:"+ this.SecWebSocketKey + "\r\n"
                    + "Sec-WebSocket-Version:" + this.SecWebSocketVersion + "\r\n\r\n";

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);

            if (!useSSL)
            {
                socket.Send(buffer, buffer.Length, System.Net.Sockets.SocketFlags.None);
                return;
            }
            ns = new System.Net.Sockets.NetworkStream(socket, true);
            ns.ReadTimeout = 15000;
            ns.ReadTimeout = 15000;
            sslStream = new System.Net.Security.SslStream(ns, false, new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate),null);
            sslStream.ReadTimeout = 15000;
            sslStream.WriteTimeout = 15000;

            try
            {
                sslStream.AuthenticateAsClient(TargetURI.Host);
            }
            catch (System.Security.Authentication.AuthenticationException e)
            {
                lastException = e;
                Disconnect();
                return;
            }

            sslStream.Write(buffer);

        }
        public bool SendData(IWebSocketDataFrame data)
        {
            if (IsConnencted)
            {
                byte[] buffer = data.GetBytes();
                if (useSSL)
                {
                    sslStream.Write(buffer);
                }
                else
                {
                    socket.Send(buffer, buffer.Length, System.Net.Sockets.SocketFlags.None);
                }
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
