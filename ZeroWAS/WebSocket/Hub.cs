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

        public void SendData(string message)
        {
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        channels[index].SendToCurrentChannel(message);
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendData(string message, TUser toUser)
        {
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        channels[index].SendToCurrentChannel(message, toUser);
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendData(string message, TUser toUser, IWebSocketChannel<TUser> toChannel)
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
                            channels[index].SendToCurrentChannel(message, toUser);
                            return;
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendData(string message, IWebSocketChannel<TUser> toChannel)
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
                            channels[index].SendToCurrentChannel(message);
                            return;
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendData(string message, IWebSocketChannel<TUser> toChannel, TUser toUser)
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
                            channels[index].SendToCurrentChannel(message, toUser);
                            return;
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }


        public void SendBinaryData(byte[] binaryData)
        {
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        channels[index].SendBinaryDataToCurrentChannel(binaryData);
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendBinaryData(byte[] binaryData, TUser toUser)
        {
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        channels[index].SendBinaryDataToCurrentChannel(binaryData, toUser);
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendBinaryData(byte[] binaryData, TUser toUser, IWebSocketChannel<TUser> toChannel)
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
                            channels[index].SendBinaryDataToCurrentChannel(binaryData, toUser);
                            return;
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendBinaryData(byte[] binaryData, IWebSocketChannel<TUser> toChannel)
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
                            channels[index].SendBinaryDataToCurrentChannel(binaryData);
                            return;
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendBinaryData(byte[] binaryData, IWebSocketChannel<TUser> toChannel, TUser toUser)
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
                            channels[index].SendBinaryDataToCurrentChannel(binaryData, toUser);
                            return;
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }

        public void SendControlFrame(ControlOpcodeEnum opcode)
        {
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        channels[index].SendControlFrameToCurrentChannel(opcode);
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendControlFrame(ControlOpcodeEnum opcode, TUser toUser)
        {
            lock (_channelsLock)
            {
                int count = channels.Count;
                int index = 0;
                while (index < count)
                {
                    try
                    {
                        channels[index].SendControlFrameToCurrentChannel(opcode, toUser);
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendControlFrame(ControlOpcodeEnum opcode, TUser toUser, IWebSocketChannel<TUser> toChannel)
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
                            channels[index].SendControlFrameToCurrentChannel(opcode, toUser);
                            return;
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendControlFrame(ControlOpcodeEnum opcode, IWebSocketChannel<TUser> toChannel)
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
                            channels[index].SendControlFrameToCurrentChannel(opcode);
                            return;
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }
        public void SendControlFrame(ControlOpcodeEnum opcode, IWebSocketChannel<TUser> toChannel, TUser toUser)
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
                            channels[index].SendControlFrameToCurrentChannel(opcode, toUser);
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
                    DataFrame df = new DataFrame(task.Content,task.ContentType);
                    task.Accepter.Write(df.GetBytes());
                }
                else
                {
                    System.Collections.Generic.List<byte> buffer = new System.Collections.Generic.List<byte>();
                    buffer.Add(0);//开始符
                    buffer.AddRange(task.Content);
                    buffer.Add(255);//结束符
                    task.Accepter.Write(buffer.ToArray());
                }
            }
            catch(Exception ex) {
                Console.WriteLine("【WS发送时异常】Message={0}&TargetSite={1}&StackTrace=\r\n{2}", ex.Message, ex.TargetSite, ex.StackTrace);
            }
        }

    }
}
