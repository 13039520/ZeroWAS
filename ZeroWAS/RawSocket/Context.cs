using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    public class Context<TUser> : IRawSocketContext<TUser>
    {
        long _ClinetId;
        IHttpRequest _UpgradeInfo;
        TUser _User;
        IRawSocketChannel<TUser> _Channel;
        IHttpConnection<TUser> _Accepter;
        IWebApplication _Server;

        public long ClinetId { get { return _ClinetId; } }
        public IHttpRequest UpgradeInfo { get { return _UpgradeInfo; } }
        public TUser User { get { return _User; } }
        public IRawSocketChannel<TUser> Channel { get { return _Channel; } }
        public IWebApplication Server { get; }

        public Context(IRawSocketChannel<TUser> channel, IHttpRequest upgradeInfo, IHttpConnection<TUser> accepter, IWebApplication server)
        {
            _Channel = channel;
            _UpgradeInfo = upgradeInfo;
            _Accepter = accepter;
            _ClinetId = accepter.ClinetId;
            _User = accepter.User;
            _Server = server;
        }

        public void SendData(IRawSocketData data)
        {
            Channel.SendToCurrentChannel(data);
        }
        public void SendData(IRawSocketData data, TUser toUser)
        {
            Channel.SendToCurrentChannel(data, toUser);
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
            Common.SocketManager<TUser>.DisconnectRSByUser(user);
        }


    }
}
