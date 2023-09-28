using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IWebSocketContext<TUser>
    {
        long ClinetId { get; }
        TUser User { get; }
        IHttpRequest UpgradeInfo { get; }
        IWebSocketChannel<TUser> Channel { get; }

        IWebApplication Server { get; }
        object GetService(Type serviceType);

        void SendData(IWebSocketDataFrame frame);
        void SendData(IWebSocketDataFrame frame, TUser toWSUser);
        void SendData(IWebSocketDataFrame frame, IWebSocketChannel<TUser> toChannel);
        void SendData(IWebSocketDataFrame frame, IWebSocketChannel<TUser> toChannel, TUser toWSUser);

        void Disconnected();
        void Disconnected(Exception ex);
        void Disconnected(TUser user, Exception ex);

    }
}
