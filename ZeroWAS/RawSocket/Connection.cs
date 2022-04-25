using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    /// <summary>
    /// 原始Socket连接
    /// </summary>
    public class Connection<TUser>
    {
        IRawSocketChannel<TUser> _Channel;
        IRawSocketContext<TUser> _Context;
        private IHttpRequest _HttpRequest;
        public IHttpRequest HttpRequest { get { return _HttpRequest; } }
        private IHttpConnection<TUser> _SocketAccepter;
        public IHttpConnection<TUser> SocketAccepter
        {
            get { return _SocketAccepter; }
        }
        private IWebApplication _HttpServer;
        public IWebApplication HttpServer
        {
            get { return _HttpServer; }
        }
        private Handlers<TUser>.ConnectedHandler _OnConnectedHandler;
        private Handlers<TUser>.DisconnectedHandler _OnDisconnectedHandler;
        private Handlers<TUser>.ReceivedHandler _OnReceivedHandler;
        private bool _HasOnReceivedHandler = false;
        private DataPacker packer;
        private long rsClinetId = 0;
        private bool isDisconnected = false;

        public Connection(IWebApplication httpServer, IHttpConnection<TUser> socketAccepter, IHttpRequest httpRequest, IRawSocketChannel<TUser> channel)
        {
            _Channel = channel;
            _HttpServer = httpServer;
            _SocketAccepter = socketAccepter;
            _HttpRequest = httpRequest;
            _Context = new Context<TUser>(_Channel, HttpRequest, _SocketAccepter, httpServer);
            if (_Channel.Handlers != null)
            {
                _OnConnectedHandler = _Channel.Handlers.OnConnectedHandler;
                _OnDisconnectedHandler = _Channel.Handlers.OnDisconnectedHandler;
                _OnReceivedHandler = _Channel.Handlers.OnReceivedHandler;

                _HasOnReceivedHandler = _OnReceivedHandler != null;
            }
            this.rsClinetId = socketAccepter.ClinetId;
            packer = new DataPacker();
            _SocketAccepter.OnDisposed += _SocketAccepter_OnDisposed;
        }

        private void _SocketAccepter_OnDisposed(System.Exception ex)
        {
            if (isDisconnected) { return; }
            if (_OnDisconnectedHandler != null)
            {
                try
                {
                    _OnDisconnectedHandler(_Context, ex);
                }
                catch { }
            }
        }

        public void HandshakeStart()
        {
            HandshakeFinished();
        }
        public void Received(byte[] bytes)
        {
            Read(bytes);
        }
        private void Read(byte[] bytes)
        {
            try
            {
                List<IRawSocketData> rs = packer.Decode(bytes);
                if (rs.Count < 1) { return; }
                foreach(IRawSocketData data in rs)
                {
                    if (_HasOnReceivedHandler)
                    {
                        try
                        {
                            _OnReceivedHandler(_Context, data);
                        }
                        catch { }
                    }
                }
            }
            catch(Exception ex)
            {
                CloseSocket(ex);
            }
        }
        private void CloseSocket(Exception ex)
        {
            if (isDisconnected) { return; }
            isDisconnected = true;
            if (_OnDisconnectedHandler != null)
            {
                try
                {
                    _OnDisconnectedHandler(_Context, ex);
                }
                catch { }
            }
            SocketAccepter.Dispose();
        }

        private void HandshakeFinished()
        {
            if (_OnConnectedHandler != null)
            {
                try
                {
                    AuthResult<TUser> rSAuthResult = _OnConnectedHandler(HttpServer, HttpRequest, _Channel.Path);
                    if (rSAuthResult == null)
                    {
                        throw new Exception("Authentication failed");
                    }
                    _SocketAccepter.User = rSAuthResult.User;
                    _SocketAccepter.RawSocketChannelPath = _Channel.Path;
                    _Context = new Context<TUser>(_Channel, HttpRequest, _SocketAccepter, HttpServer);
                    bool hasWriteData = !string.IsNullOrEmpty(rSAuthResult.WriteData);

                    //握手完成：
                    var handshakeCompleted= new Data { Type = 1, Content = new byte[] { 79, 75 } };//79,75=O,K=OK
                    if (!rSAuthResult.IsOk)
                    {
                        handshakeCompleted.Content = System.Text.Encoding.UTF8.GetBytes(hasWriteData ? rSAuthResult.WriteData : "Authentication failed");
                    }
                    var buffer = DataPacker.Encode(handshakeCompleted);
                    _SocketAccepter.Write(buffer);

                    if (rSAuthResult.IsOk)//通过验证
                    {
                        if (hasWriteData)
                        {
                            buffer = DataPacker.Encode(new Data { Type = 1, Content = System.Text.Encoding.UTF8.GetBytes(rSAuthResult.WriteData) });
                            _SocketAccepter.Write(buffer);
                        }
                    }
                    else//没有通过验证
                    {
                        System.Threading.Thread.Sleep(200);
                        throw new Exception(string.IsNullOrEmpty(rSAuthResult.WriteData) ? "Authentication failed" : rSAuthResult.WriteData);
                    }
                }
                catch (Exception ex)
                {
                    CloseSocket(ex);
                    return;
                }
            }
            else
            {
                CloseSocket(new Exception("OnNewConnectionHandler method is missing"));
                return;
            }
        }

    }
}
