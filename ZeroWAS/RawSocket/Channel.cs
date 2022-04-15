using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    public class Channel<TUser> : IRawSocketChannel<TUser>
    {
        string path;
        IRawSocketHub<TUser> hub;
        IRawSocketHandlers<TUser> _Handlers;
        public string Path { get { return path; } }
        public IRawSocketHandlers<TUser> Handlers { get { return _Handlers; } set { _Handlers = value; } }

        public Channel(string path, IRawSocketHub<TUser> hub)
        {
            this.path = path;
            this.hub = hub;
        }

        public void AddPushTask(PushTask<TUser> task)
        {
            hub.AddPushTask(task);
        }
        public void SendToCurrentChannel(IRawSocketData data) {
            Common.SocketManager<TUser>.ForeachRS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.RawSocketChannelPath == this.Path)
                {
                    AddPushTask(new PushTask<TUser> { Content = DataPacker.Encode(data), Accepter = accepter });
                }
                return true;//继续
            }));
        }
        public void SendToCurrentChannel(IRawSocketData data, TUser toUser) {
            Common.SocketManager<TUser>.ForeachRS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.User.Equals(toUser) && accepter.RawSocketChannelPath == this.Path)
                {
                    AddPushTask(new PushTask<TUser> { Content = DataPacker.Encode(data), Accepter = accepter });
                    //return false;//不能中断：因为会存在 一个用户 在 不同地方 登录了同一个频道
                }
                return true;//继续
            }));
        }

        public void SendToHub(IRawSocketData data) {
            hub.SendData(data);
        }
        public void SendToHub(IRawSocketData data, TUser toUser) {
            hub.SendData(data, toUser);
        }
        public void SendToHub(IRawSocketData data, TUser toUser, IRawSocketChannel<TUser> toChannel) {
            hub.SendData(data, toUser, toChannel);
        }
        public void SendToHub(IRawSocketData data, IRawSocketChannel<TUser> toChannel) {
            hub.SendData(data, toChannel);
        }
        public void SendToHub(IRawSocketData data, IRawSocketChannel<TUser> toChannel, TUser toUser) {
            hub.SendData(data, toChannel, toUser);
        }

    }
}
