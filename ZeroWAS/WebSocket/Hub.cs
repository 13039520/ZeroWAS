using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.WebSocket
{
    public class Hub<TUser>: IWebSocketHub<TUser>
    {
        static object _channelsLock = new object();
        static List<IWebSocketChannel<TUser>> channels = new List<IWebSocketChannel<TUser>>();
        static object _queueLock = new object();
        static Queue<PushTask<TUser>> queue = new Queue<PushTask<TUser>>();
        System.Threading.Thread thread = null;
        bool hasChannel = false;

        public bool HasChannel { get { return hasChannel; } }
        public bool ChannelAdd(string path, IWebSocketHandlers<TUser> handler)
        {
            if (string.IsNullOrEmpty(path)|| path[0] != '/') { return false; }
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    if (string.Equals(path, channels[index].Path, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                Channel<TUser> channel = new Channel<TUser>(path, this);
                if (handler != null)
                {
                    channel.Handlers = handler;
                }
                channels.Add(channel);
                if (!hasChannel)
                {
                    hasChannel = true;
                    BeginSendThread();
                }
                return true;
            }
        }
        public bool ChannelRemove(string path)
        {
            if (string.IsNullOrEmpty(path) || path[0] != '/') { return false; }
            IWebSocketChannel<TUser> channel = null;
            bool hasIndex = false;
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    if (string.Equals(path, channels[index].Path, StringComparison.OrdinalIgnoreCase))
                    {
                        hasIndex = true;
                        break;
                    }
                    index++;
                }
                if (hasIndex)
                {
                    channel = channels[index];
                    channels.RemoveAt(index);
                }
            }
            if (!hasIndex) { return false; }
            if (channel != null)
            {
                channel.DisconnectedUsers();
            }
            return true;
        }
        public IWebSocketChannel<TUser> ChannelSerach(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                lock (_channelsLock)
                {
                    int index = 0;
                    int count = channels.Count;
                    while (index < count)
                    {
                        if (path.Equals(channels[index].Path, StringComparison.OrdinalIgnoreCase))
                        {
                            return channels[index];
                        }
                        index++;
                    }
                }
            }
            return null;
        }
        public void AddPushTask(PushTask<TUser> task)
        {
            if (!HasChannel) { return; }
            if (task != null && task.Accepter != null && task.Accepter.SocketType == Common.SocketTypeEnum.WebSocket)
            {
                lock (_queueLock)
                {
                    queue.Enqueue(task);
                }
            }
        }

        public void SendData(IWebSocketDataFrame frame)
        {
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        channels[index].SendToCurrentChannel(frame);
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendData(IWebSocketDataFrame frame, TUser toUser)
        {
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        channels[index].SendToCurrentChannel(frame, toUser);
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendData(IWebSocketDataFrame frame, TUser toUser, IWebSocketChannel<TUser> toChannel)
        {
            if (toChannel == null) { return; }
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        if (channels[index] == toChannel)
                        {
                            channels[index].SendToCurrentChannel(frame, toUser);
                            return;
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendData(IWebSocketDataFrame frame, IWebSocketChannel<TUser> toChannel)
        {
            if (toChannel == null) { return; }
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        if (channels[index] == toChannel)
                        {
                            channels[index].SendToCurrentChannel(frame);
                            return;
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendData(IWebSocketDataFrame frame, IWebSocketChannel<TUser> toChannel, TUser toUser)
        {
            if (toChannel == null) { return; }
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        if (channels[index] == toChannel)
                        {
                            channels[index].SendToCurrentChannel(frame, toUser);
                            return;
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }

        private void BeginSendThread()
        {
            thread = new System.Threading.Thread(SendThreadStart);
            thread.IsBackground = true;
            thread.Start();
        }
        private void SendThreadStart()
        {
            int sleep = 1000;
            while (true)
            {
                try
                {

                    PushTask<TUser> task = null;
                    lock (_queueLock)
                    {
                        if (queue.Count > 0)
                        {
                            task = queue.Dequeue();
                        }
                    }
                    if (task != null)
                    {
                        Send(task);
                        continue;
                    }
                    else
                    {
                        sleep = 100;
                    }
                }
                catch { sleep = 1000; }

                System.Threading.Thread.Sleep(sleep);
            }
        }
        private void Send(PushTask<TUser> task)
        {
            try
            {
                if (task.Accepter.IsDataMasked)
                {
                    task.Accepter.Write(task.Frame.GetBytes());
                }
                else
                {
                    System.Collections.Generic.List<byte> buffer = new System.Collections.Generic.List<byte>();
                    buffer.Add(0);//开始符
                    buffer.AddRange(task.Frame.GetBytes());
                    buffer.Add(255);//结束符
                    task.Accepter.Write(buffer.ToArray());
                }
            }
            catch(Exception ex) {
                Console.WriteLine("【WS发送时异常】Message={0}&TargetSite={1}&StackTrace=\r\n{2}", ex.Message, ex.TargetSite, ex.StackTrace);
            }
        }

        public void DisconnectedUsers(IWebSocketChannel<TUser> channel)
        {
            channel.DisconnectedUsers();
        }
        public void DisconnectedUser(TUser user)
        {
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        channels[index].DisconnectedUser(user);
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void DisconnectedUser(TUser user, IWebSocketChannel<TUser> channel)
        {
            channel.DisconnectedUser(user);
        }

    }
}
