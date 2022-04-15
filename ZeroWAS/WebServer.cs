using System;
using System.Collections.Generic;

namespace ZeroWAS
{
    public class WebServer<TUser> : IWebServer<TUser>
    {
        private List<IHttpHandler> _HttpHandler = new List<IHttpHandler>();
        private static object _HttpHandlerLock = new object();
        private long clinetId = 0;
        private static object clinetIdLock = new object();
        private int _backlog;
        private System.Net.Sockets.Socket _listenSocket;
        private bool _isDisposed = false;
        private bool _isRunning = false;
        private bool _hasResponseEndHandler = false;
        private IWebApplication _WebApp;
        private IWebSocketHub<TUser> _WebSocketHub = new WebSocket.Hub<TUser>();
        private IRawSocketHub<TUser> _RawSocketHub = new RawSocket.Hub<TUser>();
        private System.Security.Cryptography.X509Certificates.X509Certificate2 _x509Certificate2 = null;

        public IWebSocketHub<TUser> WebSocketHub { get { return _WebSocketHub; } }
        public IRawSocketHub<TUser> RawSocketHub { get { return _RawSocketHub; } }
        public IWebApplication WebApp { get { return _WebApp; } }


        public WebServer(int backlog): this(backlog, new WebApplication())
        {
            
        }
        public WebServer(int backlog, IWebApplication httpServer)
        {
            if (httpServer == null)
            {
                httpServer = new WebApplication();
            }

            _WebApp = httpServer;

            _backlog = backlog < 1 ? 500 : backlog;
            if (httpServer.UseHttps)
            {
                _x509Certificate2 = new System.Security.Cryptography.X509Certificates.X509Certificate2(httpServer.PFXCertificateFilePath, httpServer.PFXCertificatePassword);
            }
            _listenSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        }

        public void ListenStart()
        {
            if (_isRunning) { return; }
            if (_isDisposed)
            {
                throw new Exception("WebApp is disposed");
            }
            _isRunning = true;


            _hasResponseEndHandler = WebApp.OnResponseEndHandler != null;

            _listenSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(_WebApp.ListenIP), _WebApp.ListenPort));
            _listenSocket.Listen(_backlog);

            System.Threading.Thread threadAccept = new System.Threading.Thread(SocketAccept);
            threadAccept.IsBackground = true;
            threadAccept.Start();

            System.Threading.Thread httpCleanUp = new System.Threading.Thread(ConnectionCleanup);
            httpCleanUp.IsBackground = true;
            httpCleanUp.Start();

        }

        void SocketAccept()
        {
            while (_isRunning)
            {
                System.Net.Sockets.Socket client = null;
                try
                {
                    client = _listenSocket.Accept();
                    client.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.SendTimeout, 1000);
                    System.Threading.Thread thread = new System.Threading.Thread(ReceiveData);
                    thread.IsBackground = true;
                    thread.Start(client);
                }
                catch
                { }
            }
            Dispose();
        }
        void RawStreamReceivedHandler(IHttpConnection<TUser> Connection, IHttpDataReceiver receiver, byte[] bytes)
        {
            bool isError = false;
            switch (Connection.SocketType)
            {
                case Common.SocketTypeEnum.WebSocket:
                    WebSocket.Connection<TUser> ws = Common.SocketManager<TUser>.GetWSConnection(Connection);
                    if (ws != null)
                    {
                        try
                        {
                            ws.Received(bytes);
                        }
                        catch {
                            isError = true;
                        }
                    }
                    else
                    {
                        isError = true;
                    }
                    break;
                case Common.SocketTypeEnum.RawSocket:
                    RawSocket.Connection<TUser> rs = Common.SocketManager<TUser>.GetRSConnection(Connection);
                    if (rs != null)
                    {
                        try
                        {
                            rs.Received(bytes);
                        }
                        catch
                        {
                            isError = true;
                        }
                    }
                    else
                    {
                        isError = true;
                    }
                    break;
                case Common.SocketTypeEnum.Http:
                    bool isCompleted = receiver.Receive(bytes);
                    if (isCompleted)
                    {
                        Connection.HttpRequestCount++;
                        Connection.LastActivityTime = DateTime.Now;

                        var req = receiver.RequestData;

                        #region -- 升级成WebSocket --
                        if (req.Method == "GET" && req.Header["Upgrade"] == "websocket" && WebSocketHub.HasChannel)
                        {
                            string uri = req.URI.AbsolutePath;
                            int index = uri.IndexOf('?');
                            if (index > 0)
                            {
                                uri = uri.Substring(0, index);
                            }
                            var channel = WebSocketHub.ChannelSerach(uri);
                            if (channel != null)
                            {
                                Common.SocketManager<TUser>.UpgradeWebSocket(Connection, _WebApp, channel, req);
                                return;
                            }
                        }
                        if (req.Method == "GET" && req.Header["Upgrade"] == "rawsocket" && RawSocketHub.HasChannel)
                        {
                            string uri = req.URI.AbsolutePath;
                            int index = uri.IndexOf('?');
                            if (index > 0)
                            {
                                uri = uri.Substring(0, index);
                            }
                            var channel = RawSocketHub.ChannelSerach(uri);
                            if (channel != null)
                            {
                                Common.SocketManager<TUser>.UpgradeRawSocket(Connection, _WebApp, channel, req);
                                return;
                            }
                        }
                        #endregion

                        var res = new Http.Response<TUser>(Connection, _WebApp, req, receiver, _hasResponseEndHandler, new Http.Response<TUser>.EndHandler(ResposeEnd));

                        var handler = FindHttpHandler(req);
                        if (handler != null)
                        {
                            handler.ProcessRequest(new Http.Context(_WebApp, req, res));
                        }
                        else
                        {
                            System.IO.FileInfo staticFile = _WebApp.GetStaticFile(req.URI.AbsolutePath);
                            if (staticFile != null)
                            {
                                res.WriteStaticFile(staticFile);
                            }
                            else
                            {
                                WebApp.OnRequestReceivedHandler(new Http.Context(_WebApp, req, res));
                            }
                        }
                    }
                    else
                    {
                        if (receiver.ReceiveErrorHttpStatus != Http.Status.Continue)//接收分析时发生错误
                        {
                            var req = receiver.RequestData;
                            if (req == null)
                            {
                                req = new Http.Request();
                            }
                            //输出错误
                            var res = new Http.Response<TUser>(Connection, _WebApp, req, receiver, _hasResponseEndHandler, new Http.Response<TUser>.EndHandler(ResposeEnd));
                            res.StatusCode = receiver.ReceiveErrorHttpStatus;
                            res.End();
                            isError = true;
                        }
                    }
                    break;
            }
            
            
            if (isError)
            {
                Connection.Dispose();
            }
        }
        void RawStreamErrorHandler(object sender, Exception ex)
        {
            IHttpConnection<TUser> Connection = sender as IHttpConnection<TUser>;
            Connection.Dispose();
            Console.WriteLine("Message={0}&TargetSite={1}&StackTrace=\r\n{2}", ex.Message, ex.TargetSite, ex.StackTrace);
        }
        void ReceiveData(object obj)
        {
            System.Net.Sockets.Socket socket = (System.Net.Sockets.Socket)obj;
            Http.Connection<TUser> connection = new Http.Connection<TUser>(
                socket,
                _WebApp,
                _x509Certificate2);
            lock (clinetIdLock)
            {
                clinetId++;
                connection.ClinetId = clinetId;
            }
            connection.OnRawStreamReceivedHandler = new Http.Handlers.RawStreamReceivedHandler<TUser>(RawStreamReceivedHandler);
            connection.OnErrorHandler = new Http.Handlers.ErrorHandler(RawStreamErrorHandler);
            connection.Working();
        }
        void ConnectionCleanup()
        {
            DateTime last = DateTime.Now;
            int limitSeconds = 15;
            while (_isRunning)
            {
                DateTime now = DateTime.Now;
                var ts = (now - last).TotalSeconds;
                if (ts < limitSeconds)
                {
                    System.Threading.Thread.Sleep(1000);
                    continue;
                }
                try
                {
                    //移除并断开15秒内没有活动的http连接
                    Common.SocketManager<TUser>.DisconnectHttpSocket(now.AddSeconds(-15));
                }
                catch { }
                last = DateTime.Now;
                System.Threading.Thread.Sleep(1000);
            }
        }
        void ResposeEnd(IHttpConnection<TUser> client, byte[] resBytes, Http.Status status, IHttpProcessingResult result, IHttpDataReceiver httpDataReceiver)
        {
            if (resBytes != null && resBytes.Length > 0)
            {
                client.Write(resBytes);
            }
            if (httpDataReceiver != null)
            {
                httpDataReceiver.CleanUp();
            }
            if (WebApp.OnResponseEndHandler != null)
            {
                try
                {
                    WebApp.OnResponseEndHandler.Invoke(result);
                }
                catch { }
            }
        }
        
        
        public void AddHttpHandler(IHttpHandler handler)
        {
            if (handler == null || string.IsNullOrEmpty(handler.Key)) { return; }
            lock (_HttpHandlerLock)
            {
                int count = _HttpHandler.Count;
                int index = 0;
                while (index < count)
                {
                    if (string.Equals(handler.Key, _HttpHandler[index].Key, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return;
                    }
                    index++;
                }
                _HttpHandler.Add(handler);
            }
        }
        public IHttpHandler FindHttpHandler(IHttpRequest req)
        {
            IHttpHandler reval = null;
            string path = req.URI.AbsolutePath;
            string suffix = System.IO.Path.GetExtension(path);
            bool hasExtension = !string.IsNullOrEmpty(suffix);
            string dirPath;
            int charIndex = path.LastIndexOf('/');
            if (charIndex > -1)
            {
                dirPath = path.Substring(0, charIndex + 1);
            }
            else
            {
                dirPath = "/";
            }
            dirPath=dirPath.ToLower();

            lock (_HttpHandlerLock)
            {
                //1.先检查路径
                int count = _HttpHandler.Count;
                int index = 0;
                List<IHttpHandler> temp = new List<IHttpHandler>();
                while (index < count)
                {
                    string[] ps = _HttpHandler[index].BasePath;
                    if (ps == null || ps.Length < 1)
                    {
                        continue;
                    }
                    int count2 = ps.Length;
                    int index2 = 0;
                    while (index2 < count2)
                    {
                        if (dirPath.IndexOf(ps[index2].ToLower()) == 0)
                        {
                            temp.Add(_HttpHandler[index]);
                            break;
                        }
                        index2++;
                    }
                    index++;
                }
                count = temp.Count;
                //路径不匹配
                if (count < 1) { return CheckHttpHandler(reval,suffix); }
                //没有扩展名
                if (!hasExtension)
                {
                    //只有一个结果
                    if (count < 2) { return CheckHttpHandler(temp[0],suffix); }
                    //返回订阅路径最少的接口对象
                    int len = temp[0].BasePath.Length;
                    int index3 = 0;
                    for (int i = 1; i < count; i++)
                    {
                        int t = temp[i].BasePath.Length;
                        if (t > len)
                        {
                            continue;
                        }
                        index3 = i;
                        len = t;
                    }
                    return CheckHttpHandler(temp[index3],suffix);
                }
                //2.检查后缀名
                index = count - 1;
                while (index > -1)
                {
                    string[] ss = _HttpHandler[index].Suffix;
                    if (ss == null || ss.Length < 1)
                    {
                        temp.RemoveAt(index);
                    }
                    else
                    {
                        int count4 = ss.Length;
                        int index4 = 0;
                        bool exists = false;
                        while (index4 < count4)
                        {
                            if (exists = string.Equals(ss[index4], suffix, StringComparison.CurrentCultureIgnoreCase))
                            {
                                break;
                            }
                            index4++;
                        }
                        if (!exists)
                        {
                            temp.RemoveAt(index);
                        }
                    }
                    index--;
                }
                count = temp.Count;
                //没有结果
                if (count < 1) { return CheckHttpHandler(reval,suffix); }
                //只有一个结果
                if (count < 2) { return CheckHttpHandler(temp[0],suffix); }
                //有多个结果
                int index5 = 0;
                int len5 = temp[0].Suffix.Length;
                for (int i = 0; i < count; i++)
                {
                    int t= temp[i].Suffix.Length;
                    if (t > len5)
                    {
                        continue;
                    }
                    index5 = i;
                    len5 = t;
                }
                return CheckHttpHandler(temp[index5],suffix);
            }
        }
        private IHttpHandler CheckHttpHandler(IHttpHandler handler, string suffix)
        {
            if (handler == null) { return null; }
            if (!string.IsNullOrEmpty(suffix))
            {
                if (ItemExists(handler.Suffix, suffix))
                {
                    return handler;
                }
                return null;
            }
            else
            {
                if (ItemExists(handler.Suffix, ".*"))
                {
                    return handler;
                }
                return null;
            }
        }
        private bool ItemExists(string[] source, string item)
        {
            int len = source.Length;
            int i = 0;
            while (i < len)
            {
                if (string.Equals(source[i], item, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
                i++;
            }
            return false;
        }


        public void Dispose()
        {
            if (_isDisposed) { return; }
            _isRunning = false;
            _isDisposed = true;
            try
            {
                if (_listenSocket != null)
                {
                    Common.SocketManager<TUser>.CleanUp();
                    _listenSocket.Close();
                }
            }
            catch { }
        }



    }
}
