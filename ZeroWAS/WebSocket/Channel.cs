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
        public void SendToCurrentChannel(string message) {
            Common.SocketManager<TUser>.ForeachWS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.WebSocketChannelPath == this.Path)
                {
                    AddPushTask(new PushTask<TUser> { Content = Encoding.UTF8.GetBytes(message), ContentType = ContentOpcodeEnum.Text, Accepter = accepter });
                }
                return true;//继续
            }));
        }
        public void SendToCurrentChannel(string message, TUser toUser) {
            Common.SocketManager<TUser>.ForeachWS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.User.Equals(toUser) && accepter.WebSocketChannelPath == this.Path)
                {
                    AddPushTask(new PushTask<TUser> { Content = Encoding.UTF8.GetBytes(message), ContentType = ContentOpcodeEnum.Text, Accepter = accepter });
                    //return false;//不能中断：因为会存在 一个用户 在 不同地方 登录了同一个频道
                }
                return true;//继续
            }));
        }

        public void SendToHub(string message) {
            hub.SendData(message);
        }
        public void SendToHub(string message, TUser toUser) {
            hub.SendData(message, toUser);
        }
        public void SendToHub(string message, TUser toUser, IWebSocketChannel<TUser> toChannel) {
            hub.SendData(message, toUser, toChannel);
        }
        public void SendToHub(string message, IWebSocketChannel<TUser> toChannel) {
            hub.SendData(message, toChannel);
        }
        public void SendToHub(string message, IWebSocketChannel<TUser> toChannel, TUser toUser) {
            hub.SendData(message, toChannel, toUser);
        }


        public void SendBinaryDataToCurrentChannel(byte[] binaryData)
        {
            Common.SocketManager<TUser>.ForeachWS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.WebSocketChannelPath == this.Path)
                {
                    AddPushTask(new PushTask<TUser> { Content = binaryData, ContentType = ContentOpcodeEnum.Binary, Accepter = accepter });
                }
                return true;//继续
            }));
        }
        public void SendBinaryDataToCurrentChannel(byte[] binaryData, TUser toUser)
        {
            Common.SocketManager<TUser>.ForeachWS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.User.Equals(toUser) && accepter.WebSocketChannelPath == this.Path)
                {
                    AddPushTask(new PushTask<TUser> { Content = binaryData, ContentType = ContentOpcodeEnum.Binary, Accepter = accepter });
                    return false;//中断
                }
                return true;//继续
            }));
        }

        public void SendBinaryDataToHub(byte[] binaryData)
        {
            hub.SendBinaryData(binaryData);
        }
        public void SendBinaryDataToHub(byte[] binaryData, TUser toUser)
        {
            hub.SendBinaryData(binaryData, toUser);
        }
        public void SendBinaryDataToHub(byte[] binaryData, TUser toUser, IWebSocketChannel<TUser> toChannel)
        {
            hub.SendBinaryData(binaryData, toUser, toChannel);
        }
        public void SendBinaryDataToHub(byte[] binaryData, IWebSocketChannel<TUser> toChannel)
        {
            hub.SendBinaryData(binaryData, toChannel);
        }
        public void SendBinaryDataToHub(byte[] binaryData, IWebSocketChannel<TUser> toChannel, TUser toUser)
        {
            hub.SendBinaryData(binaryData, toChannel, toUser);
        }

        public void SendControlFrameToCurrentChannel(ControlOpcodeEnum opcode)
        {
            Common.SocketManager<TUser>.ForeachWS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.WebSocketChannelPath == this.Path)
                {
                    AddPushTask(new PushTask<TUser> { Content = new byte[0], ContentType = (ContentOpcodeEnum)opcode, Accepter = accepter });
                }
                return true;//继续
            }));
        }
        public void SendControlFrameToCurrentChannel(ControlOpcodeEnum opcode, TUser toUser)
        {
            Common.SocketManager<TUser>.ForeachWS(new Common.SocketManager<TUser>.ForeachHeadler((accepter) => {
                if (accepter.User.Equals(toUser) && accepter.WebSocketChannelPath == this.Path)
                {
                    AddPushTask(new PushTask<TUser> { Content = new byte[0], ContentType = (ContentOpcodeEnum)opcode, Accepter = accepter });
                    return false;//中断
                }
                return true;//继续
            }));
        }

        public void SendControlFrameToHub(ControlOpcodeEnum opcode)
        {
            hub.SendControlFrame(opcode);
        }
        public void SendControlFrameToHub(ControlOpcodeEnum opcode, TUser toUser)
        {
            hub.SendControlFrame(opcode, toUser);
        }
        public void SendControlFrameToHub(ControlOpcodeEnum opcode, TUser toUser, IWebSocketChannel<TUser> toChannel)
        {
            hub.SendControlFrame(opcode, toUser, toChannel);
        }
        public void SendControlFrameToHub(ControlOpcodeEnum opcode, IWebSocketChannel<TUser> toChannel)
        {
            hub.SendControlFrame(opcode, toChannel);
        }
        public void SendControlFrameToHub(ControlOpcodeEnum opcode, IWebSocketChannel<TUser> toChannel, TUser toUser)
        {
            hub.SendControlFrame(opcode, toChannel, toUser);
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
