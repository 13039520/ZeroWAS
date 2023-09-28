using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IWebSocketChannel<TUser>
    {
        string Path { get; }
        IWebSocketHandlers<TUser> Handlers { get; set; }

        void AddPushTask(WebSocket.PushTask<TUser> task);

        void SendToCurrentChannel(IWebSocketDataFrame frame);
        void SendToCurrentChannel(IWebSocketDataFrame frame, TUser toUser);

        void SendToHub(IWebSocketDataFrame frame);
        void SendToHub(IWebSocketDataFrame frame, TUser toUser);
        void SendToHub(IWebSocketDataFrame frame, TUser toUser, IWebSocketChannel<TUser> toChannel);
        void SendToHub(IWebSocketDataFrame frame, IWebSocketChannel<TUser> toChannel);
        void SendToHub(IWebSocketDataFrame frame, IWebSocketChannel<TUser> toChannel, TUser toUser);

        void DisconnectedUsers();
        void DisconnectedUser(TUser user);

    }
}
