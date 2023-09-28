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

        public void SendData(IWebSocketDataFrame frame)
        {
            Channel.AddPushTask(new PushTask<TUser> { Frame = frame, Accepter = _Accepter });
        }
        public void SendData(IWebSocketDataFrame frame, TUser toUser)
        {
            Channel.SendToCurrentChannel(frame, toUser);
        }
        public void SendData(IWebSocketDataFrame frame, IWebSocketChannel<TUser> toChannel)
        {
            Channel.SendToHub(frame, toChannel);
        }
        public void SendData(IWebSocketDataFrame frame, IWebSocketChannel<TUser> toChannel, TUser toUser)
        {
            Channel.SendToHub(frame, toChannel, toUser);
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
