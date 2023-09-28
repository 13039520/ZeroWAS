using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.WebSocket
{
    public class Channel<TUser> : IWebSocketChannel<TUser>
    {
        string path;
        IWebSocketHub<TUser> hub;
        IWebSocketHandlers<TUser> _Handlers;
        public string Path { get { return path; } }
        public IWebSocketHandlers<TUser> Handlers { get { return _Handlers; } set { _Handlers = value; } }

        public Channel(string path, IWebSocketHub<TUser> hub)
        {
            this.path = path;
            this.hub = hub;
        }

        public void AddPushTask(PushTask<TUser> task)
        {
            hub.AddPushTask(task);
        }
        public void SendToCurrentChannel(IWebSocketDataFrame frame) {
            Common.SocketManager<TUser>.ForeachWS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.WebSocketChannelPath == this.Path)
                {
                    AddPushTask(new PushTask<TUser> { Frame = frame, Accepter = accepter });
                }
                return true;//继续
            }));
        }
        public void SendToCurrentChannel(IWebSocketDataFrame frame, TUser toUser) {
            Common.SocketManager<TUser>.ForeachWS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.User.Equals(toUser) && accepter.WebSocketChannelPath == this.Path)
                {
                    AddPushTask(new PushTask<TUser> { Frame = frame, Accepter = accepter });
                    //return false;//不能中断：因为会存在 一个用户 在 不同地方 登录了同一个频道
                }
                return true;//继续
            }));
        }

        public void SendToHub(IWebSocketDataFrame frame) {
            hub.SendData(frame);
        }
        public void SendToHub(IWebSocketDataFrame frame, TUser toUser) {
            hub.SendData(frame, toUser);
        }
        public void SendToHub(IWebSocketDataFrame frame, TUser toUser, IWebSocketChannel<TUser> toChannel) {
            hub.SendData(frame, toUser, toChannel);
        }
        public void SendToHub(IWebSocketDataFrame frame, IWebSocketChannel<TUser> toChannel) {
            hub.SendData(frame, toChannel);
        }
        public void SendToHub(IWebSocketDataFrame frame, IWebSocketChannel<TUser> toChannel, TUser toUser) {
            hub.SendData(frame, toChannel, toUser);
        }

        public void DisconnectedUsers()
        {
            List<IHttpConnection<TUser>> users = new List<IHttpConnection<TUser>>();
            Common.SocketManager<TUser>.ForeachRS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.WebSocketChannelPath == this.Path)
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
        public void DisconnectedUser(TUser user)
        {
            List<IHttpConnection<TUser>> users = new List<IHttpConnection<TUser>>();
            Common.SocketManager<TUser>.ForeachRS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.User.Equals(user) && accepter.WebSocketChannelPath == this.Path)
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
