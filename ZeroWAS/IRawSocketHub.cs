using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IRawSocketHub<TUser>
    {

        bool HasChannel { get; }
        bool ChannelAdd(string path, IRawSocketHandlers<TUser> handler);
        IRawSocketChannel<TUser> ChannelSerach(string wsPath);
        void AddPushTask(RawSocket.PushTask<TUser> task);

        void SendData(IRawSocketData data);
        void SendData(IRawSocketData data, TUser toWSUser);
        void SendData(IRawSocketData data, TUser toWSUser, IRawSocketChannel<TUser> toWSChannel);
        void SendData(IRawSocketData data, IRawSocketChannel<TUser> toWSChannel);
        void SendData(IRawSocketData data, IRawSocketChannel<TUser> toWSChannel, TUser toWSUser);

        void DisconnectedUsers(IRawSocketChannel<TUser> channel);
        void DisconnectedUser(TUser user);
        void DisconnectedUser(TUser user, IRawSocketChannel<TUser> channel);

    }
}
