using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using ZeroWAS.Http;
using ZeroWAS.RawSocket;

namespace ZeroWAS
{
    public class WebServer<TUser> : IWebServer<TUser>
    {
        //private List<IHttpHandler> _HttpHandler = new List<IHttpHandler>();
        //private static object _HttpHandlerLock = new object();
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
        private System.Security.Cryptography.X509Certificates.X509Certificate2 _x509Cer = null;
        private Http.HttpHandlerDispatcher _httpHandlerDispatcher = null;

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
                _x509Cer = httpServer.X509Cer;
            }
            _listenSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

            _httpHandlerDispatcher = new Http.HttpHandlerDispatcher();
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
        void RawStreamReceivedHandler(IHttpConnection<TUser> connection, IHttpDataReceiver receiver, byte[] bytes)
        {
            bool isError = false;
            switch (connection.SocketType)
            {
                case Common.SocketTypeEnum.WebSocket:
                    WebSocket.Connection<TUser> ws = Common.SocketManager<TUser>.GetWSConnection(connection);
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
                    RawSocket.Connection<TUser> rs = Common.SocketManager<TUser>.GetRSConnection(connection);
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
                        IHttpRequest req = receiver.RequestData;
                        receiver.RequestData = null;
                        if (HostMatcher.Match(req.HostName, _WebApp.HostName))
                        {
                            connection.HttpRequestCount++;
                            connection.LastActivityTime = DateTime.Now;
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
                                    Common.SocketManager<TUser>.UpgradeWebSocket(connection, _WebApp, channel, req);
                                    req.Dispose();
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
                                    Common.SocketManager<TUser>.UpgradeRawSocket(connection, _WebApp, channel, req);
                                    req.Dispose();
                                    return;
                                }
                            }
                            #endregion

                            ProcessHttpRequest(connection, req);
                        }
                        else
                        {
                            ProcessError(connection, req, Status.Misdirected_Request, "Host mismatch.");
                            isError = true;
                        }
                    }
                    else
                    {
                        ProcessError(connection, receiver.RequestData, receiver.ReceiveErrorHttpStatus, receiver.ReceiveErrorMsg);
                        isError = true;
                    }
                    break;
            }
            
            
            if (isError)
            {
                connection.Dispose();
            }
        }
        void ProcessError(IHttpConnection<TUser> connection, IHttpRequest request, Http.Status status, string errorMsg)
        {
            using (var res = new Http.Response<TUser>(connection, _WebApp, request, _hasResponseEndHandler, ResposeEnd))
            {
                res.StatusCode = status;
                res.ContentType = "text/plain; charset=utf-8";
                byte[] error = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(errorMsg) ? "未知错误" : errorMsg);
                res.Write(error);
                res.End();
            }
        }
        void ProcessHttpRequest(IHttpConnection<TUser> connection, IHttpRequest req)
        {
            var res = new Http.Response<TUser>(connection, _WebApp, req, _hasResponseEndHandler, ResposeEnd);
            ThreadPool.QueueUserWorkItem(delegate (object state)
            {
                var ctx = (Http.Response<TUser>)state;
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
                res.Dispose();
                res = null;
            }, res);
        }
        void RawStreamErrorHandler(object sender, Exception ex)
        {
            IHttpConnection<TUser> connection = sender as IHttpConnection<TUser>;
            connection.Dispose();
            Console.WriteLine("Message={0}&TargetSite={1}&StackTrace=\r\n{2}", ex.Message, ex.TargetSite, ex.StackTrace);
        }
        void ReceiveData(object obj)
        {
            System.Net.Sockets.Socket socket = (System.Net.Sockets.Socket)obj;
            Http.Connection<TUser> connection = new Http.Connection<TUser>(
                socket,
                _WebApp,
                _x509Cer);
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
        void ResposeEnd(IHttpConnection<TUser> client, byte[] resBytes, Http.Status status, IHttpProcessingResult result)
        {
            if (resBytes != null && resBytes.Length > 0)
            {
                client.Write(resBytes);
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
            _httpHandlerDispatcher.Register(handler);
        }
        public IHttpHandler FindHttpHandler(IHttpRequest req)
        {
            string path = req.URI.PathAndQuery;
            return _httpHandlerDispatcher.Dispatch(path);
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
