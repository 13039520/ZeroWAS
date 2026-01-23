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
        void AddPushTask(IRawSocketPushTask<TUser> task);

        void SendData(IRawSocketSendMessage data);
        void SendData(IRawSocketSendMessage data, TUser toUser);
        void SendData(IRawSocketSendMessage data, TUser toUser, IRawSocketChannel<TUser> toChannel);
        void SendData(IRawSocketSendMessage data, IRawSocketChannel<TUser> toChannel);
        void SendData(IRawSocketSendMessage data, IRawSocketChannel<TUser> toChannel, TUser toUser);

        void DisconnectedUsers(IRawSocketChannel<TUser> channel);
        void DisconnectedUser(TUser user);
        void DisconnectedUser(TUser user, IRawSocketChannel<TUser> channel);

    }
}
