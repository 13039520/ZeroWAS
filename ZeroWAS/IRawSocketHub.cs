using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IRawSocketHub<TUser>
    {

        bool HasChannel { get; }
        bool ChannelAdd(string path, IRawSocketHandlers<TUser> handler);
        bool ChannelRemove(string path);
        IRawSocketChannel<TUser> ChannelSerach(string rsPath);
        void AddPushTask(RawSocket.PushTask<TUser> task);

        void SendData(IRawSocketData data);
        void SendData(IRawSocketData data, TUser toUser);
        void SendData(IRawSocketData data, TUser toUser, IRawSocketChannel<TUser> toChannel);
        void SendData(IRawSocketData data, IRawSocketChannel<TUser> toChannel);
        void SendData(IRawSocketData data, IRawSocketChannel<TUser> toChannel, TUser toUser);

        void DisconnectedUsers(IRawSocketChannel<TUser> channel);
        void DisconnectedUser(TUser user);
        void DisconnectedUser(TUser user, IRawSocketChannel<TUser> channel);

    }
}
