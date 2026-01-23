using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    public class Hub<TUser>: IRawSocketHub<TUser>
    {
        static object _channelsLock = new object();
        static List<IRawSocketChannel<TUser>> channels = new List<IRawSocketChannel<TUser>>();
        static object _queueLock = new object();
        static Queue<IRawSocketPushTask<TUser>> queue = new Queue<IRawSocketPushTask<TUser>>();
        System.Threading.Thread thread = null;
        bool hasChannel = false;

        public bool HasChannel { get { return hasChannel; } }
        public bool ChannelAdd(string path, IRawSocketHandlers<TUser> handlers)
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
                    index++;
                }
                Channel<TUser> channel = new Channel<TUser>(path, this);
                if (handlers != null)
                {
                    channel.Handlers = handlers;
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
            IRawSocketChannel<TUser> channel = null;
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
        public IRawSocketChannel<TUser> ChannelSerach(string path)
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
        public void AddPushTask(IRawSocketPushTask<TUser> task)
        {
            if (!HasChannel) { return; }
            if (task != null && task.Accepters != null && task.Accepters.Count>0)
            {
                lock (_queueLock)
                {
                    queue.Enqueue(task);
                }
            }
        }

        public void SendData(IRawSocketSendMessage data)
        {
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        channels[index].SendToCurrentChannel(data);
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendData(IRawSocketSendMessage data, TUser toUser)
        {
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        channels[index].SendToCurrentChannel(data, toUser);
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendData(IRawSocketSendMessage data, TUser toUser, IRawSocketChannel<TUser> toChannel)
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
                            channels[index].SendToCurrentChannel(data, toUser);
                            return;
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendData(IRawSocketSendMessage data, IRawSocketChannel<TUser> toChannel)
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
                            channels[index].SendToCurrentChannel(data);
                            return;
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendData(IRawSocketSendMessage data, IRawSocketChannel<TUser> toChannel, TUser toUser)
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
                            channels[index].SendToCurrentChannel(data, toUser);
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

                    IRawSocketPushTask<TUser> task = null;
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
        private void Send(IRawSocketPushTask<TUser> task)
        {
            try
            {
                task.Content.Read(bytes =>
                {
                    for (int i = task.Accepters.Count - 1; i >= 0; i--)
                    {
                        var item = task.Accepters[i];
                        try
                        {
                            item.Write(bytes);
                        }
                        catch
                        {
                            task.Accepters.RemoveAt(i);
                            // Console.WriteLine("被移出");
                        }
                    }
                });
            }
            finally
            {
                task.Content.End();
            }
        }


        public void DisconnectedUsers(IRawSocketChannel<TUser> channel)
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
        public void DisconnectedUser(TUser user, IRawSocketChannel<TUser> channel)
        {
            channel.DisconnectedUser(user);
        }

    }
}
