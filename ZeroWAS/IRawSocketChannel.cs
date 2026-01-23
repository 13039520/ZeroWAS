using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IRawSocketChannel<TUser>
    {
        string Path { get; }
        IRawSocketHandlers<TUser> Handlers { get; set; }

        void AddPushTask(IRawSocketPushTask<TUser> task);

        void SendToCurrentChannel(IRawSocketSendMessage data);
        void SendToCurrentChannel(IRawSocketSendMessage data, TUser toUser);

        void SendToHub(IRawSocketSendMessage data);
        void SendToHub(IRawSocketSendMessage data, TUser toUser);
        void SendToHub(IRawSocketSendMessage data, TUser toUser, IRawSocketChannel<TUser> toChannel);
        void SendToHub(IRawSocketSendMessage data, IRawSocketChannel<TUser> toChannel);
        void SendToHub(IRawSocketSendMessage data, IRawSocketChannel<TUser> toChannel, TUser toUser);

        void DisconnectedUsers();
        void DisconnectedUser(TUser user);

    }
}
