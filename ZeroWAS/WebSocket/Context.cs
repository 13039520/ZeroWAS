using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.WebSocket
{
    public class Context<TUser> : IWebSocketContext<TUser>
    {
        long _ClinetId;
        IHttpRequest _UpgradeInfo;
        TUser _User;
        IWebSocketChannel<TUser> _Channel;
        IHttpConnection<TUser> _Accepter;
        IWebApplication _Server;

        public long ClinetId { get { return _ClinetId; } }
        public IHttpRequest UpgradeInfo { get { return _UpgradeInfo; } }
        public TUser User { get { return _User; } }
        public IWebSocketChannel<TUser> Channel { get { return _Channel; } }
        public IWebApplication Server { get { return _Server; } }
        public object GetService(Type serviceType)
        {
            return Server.GetService(serviceType);
        }
        public Context(IWebSocketChannel<TUser> channel, IHttpRequest upgradeInfo, IHttpConnection<TUser> accepter, IWebApplication server)
        {
            _Channel = channel;
            _UpgradeInfo = upgradeInfo;
            _Accepter = accepter;
            _ClinetId = accepter.ClinetId;
            _User = accepter.User;
            _Server = server;
        }

        public void SendData(string message)
        {
            Channel.AddPushTask(new PushTask<TUser> { Content = Encoding.UTF8.GetBytes(message), ContentType = ContentOpcodeEnum.Text, Accepter = _Accepter });
        }
        public void SendData(string message, TUser toUser)
        {
            Channel.SendToCurrentChannel(message, toUser);
        }
        public void SendData(string message, IWebSocketChannel<TUser> toChannel)
        {
            Channel.SendToHub(message, toChannel);
        }
        public void SendData(string message, IWebSocketChannel<TUser> toChannel, TUser toUser)
        {
            Channel.SendToHub(message, toChannel, toUser);
        }
        public void SendControlFrame(ControlOpcodeEnum opcode)
        {
            Channel.SendControlFrameToCurrentChannel(opcode);
        }
        public void SendControlFrame(ControlOpcodeEnum opcode, TUser toWSUser)
        {
            Channel.SendControlFrameToCurrentChannel(opcode, toWSUser);
        }
        public void SendControlFrame(ControlOpcodeEnum opcode, IWebSocketChannel<TUser> toChannel)
        {
            Channel.SendControlFrameToHub(opcode, toChannel);
        }
        public void SendControlFrame(ControlOpcodeEnum opcode, IWebSocketChannel<TUser> toChannel, TUser toUser)
        {
            Channel.SendControlFrameToHub(opcode, toChannel, toUser);
        }
        public void SendBinaryData(byte[] binaryData)
        {
            Channel.SendBinaryDataToCurrentChannel(binaryData);
        }
        public void SendBinaryData(byte[] binaryData, TUser toWSUser)
        {
            Channel.SendBinaryDataToCurrentChannel(binaryData, toWSUser);
        }
        public void SendBinaryData(byte[] binaryData, IWebSocketChannel<TUser> toChannel)
        {
            Channel.SendBinaryDataToHub(binaryData, toChannel);
        }
        public void SendBinaryData(byte[] binaryData, IWebSocketChannel<TUser> toChannel, TUser toUser)
        {
            Channel.SendBinaryDataToHub(binaryData, toChannel, toUser);
        }
        public void Disconnected()
        {
            Disconnected(this.User, new Exception("Normal"));
        }
        public void Disconnected(Exception ex)
        {
            Disconnected(this.User, ex);
        }
        public void Disconnected(TUser user, Exception ex)
        {
            Common.SocketManager<TUser>.DisconnectWSByUser(user);
        }

        private void DisconnectedByClientId(long clinetId)
        {
            WSDisconnectedByClientId(clinetId, new Exception("Disconnected by clinetId"));
        }
        private void WSDisconnectedByClientId(long clinetId, Exception ex)
        {
            Common.SocketManager<TUser>.DisconnectWSByClientId(clinetId);
        }
        private void WSDisconnectedByUser(TUser user)
        {
            WSDisconnectedByUser(user, new Exception("Disconnected by user"));
        }
        private void WSDisconnectedByUser(TUser user, Exception ex)
        {
            Common.SocketManager<TUser>.DisconnectWSByUser(user);
        }

    }
}
