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

        void SendData(IRawSocketData data);
        void SendData(IRawSocketData data, TUser toWSUser);

        void Disconnected();
        void Disconnected(Exception ex);
        void Disconnected(TUser user, Exception ex);

    }
}
