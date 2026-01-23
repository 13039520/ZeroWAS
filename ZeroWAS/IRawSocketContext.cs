using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IRawSocketContext<TUser>
    {
        long ClinetId { get; }
        TUser User { get; }
        IHttpRequest UpgradeInfo { get; }
        IRawSocketChannel<TUser> Channel { get; }
        IWebApplication Server { get; }
        object GetService(Type serviceType);
        void SendData(IRawSocketSendMessage data);
        void SendData(IRawSocketSendMessage data, TUser toRSUser);

        void Disconnected();
        void Disconnected(Exception ex);
        void Disconnected(TUser user, Exception ex);

    }
}
