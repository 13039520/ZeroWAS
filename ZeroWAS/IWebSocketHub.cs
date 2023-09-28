using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IWebSocketHub<TUser>
    {

        bool HasChannel { get; }
        bool ChannelAdd(string path, IWebSocketHandlers<TUser> handler);
        IWebSocketChannel<TUser> ChannelSerach(string wsPath);
        void AddPushTask(WebSocket.PushTask<TUser> task);

        void SendData(IWebSocketDataFrame frame);
        void SendData(IWebSocketDataFrame frame, TUser toWSUser);
        void SendData(IWebSocketDataFrame frame, TUser toWSUser, IWebSocketChannel<TUser> toWSChannel);
        void SendData(IWebSocketDataFrame frame, IWebSocketChannel<TUser> toWSChannel);
        void SendData(IWebSocketDataFrame frame, IWebSocketChannel<TUser> toWSChannel, TUser toWSUser);

        void DisconnectedUsers(IWebSocketChannel<TUser> channel);
        void DisconnectedUser(TUser user);
        void DisconnectedUser(TUser user, IWebSocketChannel<TUser> channel);

    }
}
