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

        public void AddPushTask(IRawSocketPushTask<TUser> task)
        {
            hub.AddPushTask(task);
        }
        public void SendToCurrentChannel(IRawSocketSendMessage data) {
            List<IHttpConnection<TUser>> clients = new List<IHttpConnection<TUser>>();
            var serializedMessage = new SerializedMessage(data);
            Common.SocketManager<TUser>.ForeachRS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.RawSocketChannelPath.Equals(this.Path, StringComparison.OrdinalIgnoreCase))
                {
                    clients.Add(accepter);
                    serializedMessage.Take();
                }
                return true;//继续
            }));
            if (clients.Count > 0)
            {
                AddPushTask(new PushTask<TUser> { Accepters = clients, Content = serializedMessage });
            }
            serializedMessage.EndDispatch();
        }
        public void SendToCurrentChannel(IRawSocketSendMessage data, TUser toUser) {
            List<IHttpConnection<TUser>> clients = new List<IHttpConnection<TUser>>();
            var serializedMessage = new SerializedMessage(data);
            Common.SocketManager<TUser>.ForeachRS((accepter) => {
                if (accepter.User.Equals(toUser) && accepter.RawSocketChannelPath.Equals(this.Path, StringComparison.OrdinalIgnoreCase))
                {
                    clients.Add(accepter);
                    serializedMessage.Take();
                }
                return true;//不能中断：因为会存在 一个用户 在 不同地方 登录了同一个频道
            });
            if (clients.Count > 0)
            {
                AddPushTask(new PushTask<TUser> { Accepters = clients, Content = serializedMessage });
            }
            serializedMessage.EndDispatch();
        }

        public void SendToHub(IRawSocketSendMessage data) {
            hub.SendData(data);
        }
        public void SendToHub(IRawSocketSendMessage data, TUser toUser) {
            hub.SendData(data, toUser);
        }
        public void SendToHub(IRawSocketSendMessage data, TUser toUser, IRawSocketChannel<TUser> toChannel) {
            hub.SendData(data, toUser, toChannel);
        }
        public void SendToHub(IRawSocketSendMessage data, IRawSocketChannel<TUser> toChannel) {
            hub.SendData(data, toChannel);
        }
        public void SendToHub(IRawSocketSendMessage data, IRawSocketChannel<TUser> toChannel, TUser toUser) {
            hub.SendData(data, toChannel, toUser);
        }



        public void DisconnectedUsers()
        {
            List<IHttpConnection<TUser>> users = new List<IHttpConnection<TUser>>();
            Common.SocketManager<TUser>.ForeachRS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.RawSocketChannelPath == this.Path)
                {
                    users.Add(accepter);
                }
                return true;//继续
            }));
            foreach(var u in users)
            {
                try
                {
                    u.Dispose();
                }
                catch { }
            }
        }
        public void DisconnectedUser(TUser user)
        {
            List<IHttpConnection<TUser>> users = new List<IHttpConnection<TUser>>();
            Common.SocketManager<TUser>.ForeachRS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.User.Equals(user) && accepter.RawSocketChannelPath == this.Path)
                {
                    users.Add(accepter);
                }
                return true;//继续
            }));
            foreach (var u in users)
            {
                try
                {
                    u.Dispose();
                }
                catch { }
            }
        }

    }
}
