using System.Text;
using System.Net.Sockets;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace ZeroWAS.WebSocket
{
    public class Connection<TUser>
    {
        private bool isDataMasked;
        public bool IsDataMasked
        {
            get { return isDataMasked; }
            set { isDataMasked = value; }
        }
        IWebSocketChannel<TUser> _Channel;
        IWebSocketContext<TUser> _Context;
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

        private int MaxMessageSize;
        private string Handshake;
        private string New_Handshake;

        private byte[] ServerKey1;
        private byte[] ServerKey2;
        private long wsClinetId = 0;
        private bool isDisconnected = false;

        private DataReceiver dataReceiver = null;
        private Handlers<TUser>.ConnectedHandler _OnConnectedHandler;
        private Handlers<TUser>.DisconnectedHandler _OnDisconnectedHandler;
        private Handlers<TUser>.TextFrameReceivedHandler _OnTextFrameReceivedHandler;
        private Handlers<TUser>.ContinuationFrameReceivedHandler _OnContinuationFrameReceivedHandler;
        private Handlers<TUser>.BinaryFrameReceivedHandler _OnBinaryFrameReceivedHandler;


        public Connection(IWebApplication httpServer, IHttpConnection<TUser> socketAccepter, IHttpRequest httpRequest, IWebSocketChannel<TUser> channel)
        {
            _Channel = channel;
            _HttpServer = httpServer;
            _SocketAccepter = socketAccepter;
            _HttpRequest = httpRequest;
            if (_Channel.Handlers != null)
            {
                _OnConnectedHandler = _Channel.Handlers.OnConnectedHandler;
                _OnDisconnectedHandler = _Channel.Handlers.OnDisconnectedHandler;
                _OnTextFrameReceivedHandler = _Channel.Handlers.OnTextFrameReceivedHandler;
                _OnBinaryFrameReceivedHandler = _Channel.Handlers.OnBinaryFrameReceivedHandler;
                _OnContinuationFrameReceivedHandler = _Channel.Handlers.OnContinuationFrameReceivedHandler;
            }
            _Context = new Context<TUser>(_Channel, HttpRequest, _SocketAccepter, httpServer);
            this.wsClinetId = socketAccepter.ClinetId;
            _SocketAccepter.OnDisposed += _SocketAccepter_OnDisposed;


            MaxMessageSize = 1024*1024*4;
            dataReceiver = new DataReceiver(new DataReceiver.DataFrameCallback(DataFrameReceived), MaxMessageSize);

            Handshake = "HTTP/1.1 101 Web Socket Protocol Handshake" + Environment.NewLine;
            Handshake += "Upgrade: WebSocket" + Environment.NewLine;
            Handshake += "Connection: Upgrade" + Environment.NewLine;
            Handshake += "Sec-WebSocket-Origin: " + "{0}" + Environment.NewLine;
            Handshake += string.Format("Sec-WebSocket-Location: " + "ws://{0}{1}" + Environment.NewLine, httpServer.HostName, channel.Path);
            Handshake += Environment.NewLine;

            New_Handshake = "HTTP/1.1 101 Switching Protocols" + Environment.NewLine;
            New_Handshake += "Upgrade: WebSocket" + Environment.NewLine;
            New_Handshake += "Connection: Upgrade" + Environment.NewLine;
            New_Handshake += "Sec-WebSocket-Accept: {0}" + Environment.NewLine;
            New_Handshake += Environment.NewLine;

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

        public void Received(byte[] bytes)
        {
            dataReceiver.Received(bytes);
        }
        public void HandshakeStart()
        {
            ManageHandshake();
        }
        private void DataFrameReceived(ReceivedResult result)
        {
            if (result.Data != null)
            {
                string messageReceived = string.Empty;
                DataFrame dr = result.Data;
                try
                {
                    switch (dr.Header.OpCode)
                    {
                        case 8://close
                            _Context.SendControlFrame(ControlOpcodeEnum.Close);
                            CloseSocket(new Exception("正常断开"));
                            break;
                        case 9://ping
                            _Context.SendControlFrame(ControlOpcodeEnum.Pong);
                            Console.WriteLine("Received=>Ping");
                            break;
                        case 10://pong
                            SocketAccepter.LastActivityTime = DateTime.Now;
                            Console.WriteLine("Received=>Pong");
                            break;
                        case 0://延续帧
                            SocketAccepter.LastActivityTime = DateTime.Now;
                            if (_OnContinuationFrameReceivedHandler != null)
                            {
                                try
                                {
                                    _OnContinuationFrameReceivedHandler(_Context, dr.Content, dr.Header.FIN);
                                }
                                catch { }
                            }
                            break;
                        case 1://文本帧

                            messageReceived = dr.Text;
                            SocketAccepter.LastActivityTime = DateTime.Now;
                            if (_OnTextFrameReceivedHandler != null)
                            {
                                try
                                {
                                    _OnTextFrameReceivedHandler(_Context, messageReceived);
                                }
                                catch { }
                            }

                            break;
                        case 2://二进制帧
                            SocketAccepter.LastActivityTime = DateTime.Now;
                            if (_OnBinaryFrameReceivedHandler != null)
                            {
                                try
                                {
                                    _OnBinaryFrameReceivedHandler(_Context, dr.Content);
                                }
                                catch { }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    CloseSocket(ex);
                }
            }
            else
            {
                CloseSocket(new Exception(result.ErrorMessage));
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

        private void BuildServerPartialKey(int keyNum, string clientKey)
        {
            string partialServerKey = "";
            byte[] currentKey;
            int spacesNum = 0;
            char[] keyChars = clientKey.ToCharArray();
            foreach (char currentChar in keyChars)
            {
                if (char.IsDigit(currentChar)) partialServerKey += currentChar;
                if (char.IsWhiteSpace(currentChar)) spacesNum++;
            }
            try
            {
                currentKey = BitConverter.GetBytes((int)(Int64.Parse(partialServerKey) / spacesNum));
                if (BitConverter.IsLittleEndian) Array.Reverse(currentKey);

                if (keyNum == 1) ServerKey1 = currentKey;
                else ServerKey2 = currentKey;
            }
            catch
            {
                if (ServerKey1 != null) Array.Clear(ServerKey1, 0, ServerKey1.Length);
                if (ServerKey2 != null) Array.Clear(ServerKey2, 0, ServerKey2.Length);
            }
        }

        private byte[] BuildServerFullKey(byte[] last8Bytes)
        {
            byte[] concatenatedKeys = new byte[16];
            Array.Copy(ServerKey1, 0, concatenatedKeys, 0, 4);
            Array.Copy(ServerKey2, 0, concatenatedKeys, 4, 4);
            Array.Copy(last8Bytes, 0, concatenatedKeys, 8, 8);

            // MD5 Hash
            System.Security.Cryptography.MD5 MD5Service = System.Security.Cryptography.MD5.Create();
            return MD5Service.ComputeHash(concatenatedKeys);
        }

        private void ManageHandshake()
        {
            try
            {
                byte[] last8Bytes = new byte[8];
                System.Text.UTF8Encoding decoder = new System.Text.UTF8Encoding();

                //现在使用的是比较新的Websocket协议
                if (!string.IsNullOrEmpty(HttpRequest.Header["Sec-WebSocket-Version"]))
                {
                    this.isDataMasked = true;
                    SocketAccepter.IsDataMasked = true;

                    string acceptKey = ComputeWebSocketHandshakeSecurityHash09(HttpRequest.Header["Sec-WebSocket-Key"]);

                    New_Handshake = string.Format(New_Handshake, acceptKey);
                    byte[] newHandshakeText = Encoding.UTF8.GetBytes(New_Handshake);

                    SocketAccepter.Write(newHandshakeText);
                    HandshakeFinished();
                    return;
                }

                SocketAccepter.IsDataMasked = false;
                BuildServerPartialKey(1, HttpRequest.Header["Sec-WebSocket-Key1"]);
                BuildServerPartialKey(2, HttpRequest.Header["Sec-WebSocket-Key2"]);
                if (!string.IsNullOrEmpty(HttpRequest.Header["Origin"]))
                {
                    Handshake = string.Format(Handshake, HttpRequest.Header["Origin"]);
                }
                else
                {
                    Handshake = string.Format(Handshake, "null");
                }

                byte[] HandshakeText = Encoding.UTF8.GetBytes(Handshake);
                byte[] serverHandshakeResponse = new byte[HandshakeText.Length + 16];
                byte[] serverKey = BuildServerFullKey(last8Bytes);
                Array.Copy(HandshakeText, serverHandshakeResponse, HandshakeText.Length);
                Array.Copy(serverKey, 0, serverHandshakeResponse, HandshakeText.Length, 16);

                SocketAccepter.Write(serverHandshakeResponse);
                HandshakeFinished();

            }catch{ }
        }

        public static string ComputeWebSocketHandshakeSecurityHash09(string secWebSocketKey)
        {
            const string MagicKEY = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string secWebSocketAccept = String.Empty;
            string ret = secWebSocketKey + MagicKEY;
            SHA1 sha = SHA1.Create();
            byte[] sha1Hash = sha.ComputeHash(Encoding.UTF8.GetBytes(ret));
            secWebSocketAccept = Convert.ToBase64String(sha1Hash);
            return secWebSocketAccept;
        }

        private void HandshakeFinished()
        {
            if (_OnConnectedHandler != null)
            {
                try
                {
                    AuthResult<TUser> wSAuthResult = _OnConnectedHandler(HttpServer, HttpRequest, _Channel.Path);
                    if (wSAuthResult == null)
                    {
                        throw new Exception("Authentication failed");
                    }
                    _SocketAccepter.User = wSAuthResult.User;
                    _SocketAccepter.WebSocketChannelPath = _Channel.Path;
                    _Context = new Context<TUser>(_Channel, HttpRequest, _SocketAccepter, HttpServer);
                    _Context.SendData(string.Format("CLINETID={0}", _SocketAccepter.ClinetId), _SocketAccepter.User);
                    if (!string.IsNullOrEmpty(wSAuthResult.WriteMsg))
                    {
                        _Context.SendData(wSAuthResult.WriteMsg, _SocketAccepter.User);
                    }
                    if (!wSAuthResult.IsOk)
                    {
                        System.Threading.Thread.Sleep(200);
                        throw new Exception(string.IsNullOrEmpty(wSAuthResult.WriteMsg) ? "Authentication failed" : wSAuthResult.WriteMsg);
                    }
                }
                catch(Exception ex)
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