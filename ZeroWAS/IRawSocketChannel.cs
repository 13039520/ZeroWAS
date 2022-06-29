using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IRawSocketChannel<TUser>
    {
        string Path { get; }
        IRawSocketHandlers<TUser> Handlers { get; set; }

        void AddPushTask(RawSocket.PushTask<TUser> task);

        void SendToCurrentChannel(IRawSocketData data);
        void SendToCurrentChannel(IRawSocketData data, TUser toUser);

        void SendToHub(IRawSocketData data);
        void SendToHub(IRawSocketData data, TUser toUser);
        void SendToHub(IRawSocketData data, TUser toUser, IRawSocketChannel<TUser> toChannel);
        void SendToHub(IRawSocketData data, IRawSocketChannel<TUser> toChannel);
        void SendToHub(IRawSocketData data, IRawSocketChannel<TUser> toChannel, TUser toUser);

        void DisconnectedUsers();
        void DisconnectedUser(TUser user);

    }
}
