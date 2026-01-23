using System;
using System.Collections.Generic;
using System.IO;
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
        private MessageReceiver frameReceiver;
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
            frameReceiver = new MessageReceiver();
            frameReceiver.OnMessage += FrameReceiver_OnReceived;
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
                frameReceiver.Receive(bytes);
            }
            catch(Exception ex)
            {
                CloseSocket(ex);
            }
        }
        private void FrameReceiver_OnReceived(IRawSocketReceivedMessage frame)
        {
            if (_HasOnReceivedHandler)
            {
                try
                {
                    _OnReceivedHandler(_Context, frame);
                }
                catch { }
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
                    AuthResult<TUser> rSAuthResult = _OnConnectedHandler(HttpServer, HttpRequest, _Channel.Path, rsClinetId);
                    if (rSAuthResult == null)
                    {
                        throw new Exception("Authentication failed");
                    }
                    _SocketAccepter.User = rSAuthResult.User;
                    _SocketAccepter.RawSocketChannelPath = _Channel.Path;
                    _Context = new Context<TUser>(_Channel, HttpRequest, _SocketAccepter, HttpServer);
                    bool hasFrameContent = rSAuthResult.MessageContent != null && rSAuthResult.MessageContent.Length > 0;
                    bool hasFrameRemark=!string.IsNullOrEmpty(rSAuthResult.MessageRemark);
                    System.IO.MemoryStream content = null;
                    if(rSAuthResult.MessageContent != null && rSAuthResult.MessageContent.Length > 0)
                    {
                        content = new System.IO.MemoryStream(rSAuthResult.MessageContent);
                    }
                    string remark = string.Empty;
                    if(!string.IsNullOrEmpty(rSAuthResult.MessageRemark))
                    {
                        remark = rSAuthResult.MessageRemark;
                    }
                    if(string.IsNullOrEmpty(remark))
                    {
                        if (!rSAuthResult.IsOk)
                        {
                            remark = "Authentication failed";
                        }
                    }
                    using (var frame = new SendMessage(1, new MemoryStream(Encoding.UTF8.GetBytes("ClientId="+ rsClinetId)), string.Empty))
                    {
                        SerializedMessage msg = new SerializedMessage(frame);
                        msg.Take();
                        msg.EndDispatch();
                        msg.Read(e => {
                            _SocketAccepter.Write(e);
                        });
                        msg.End();
                    }
                    //握手完成：
                    using (var frame= new SendMessage(1, content, remark))
                    {
                        SerializedMessage msg=new SerializedMessage(frame);
                        msg.Take();
                        msg.EndDispatch();
                        msg.Read(e => {
                            _SocketAccepter.Write(e);
                        });
                        msg.End();
                    }
                    if (!rSAuthResult.IsOk)//没有通过验证
                    {
                        System.Threading.Thread.Sleep(200);
                        throw new Exception(remark);//抛出异常触发连接的断开
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
