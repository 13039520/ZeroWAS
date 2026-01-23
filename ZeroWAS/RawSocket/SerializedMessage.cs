using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using ZeroWAS.Common;

namespace ZeroWAS.RawSocket
{
    /// <summary>
    /// 封包后的序列化消息，支持多消费者引用计数
    /// </summary>
    internal sealed class SerializedMessage : IRawSocketSerializedMessage
    {
        private readonly IPayloadBuffer _buffer;

        private volatile int _takeCount = 0;
        private volatile int _dispatchEnded = 0;
        private volatile int _disposed = 0;

        private const int ChunkSize = 2048;

        /// <summary>
        /// 构造并封包 ISendMessage
        /// </summary>
        public SerializedMessage(IRawSocketSendMessage msg)
        {
            if (msg == null) throw new ArgumentNullException("msg");

            byte _type = msg.Type;
            string _remark = msg.Remark ?? string.Empty;

            byte[] remarkBytes = Encoding.UTF8.GetBytes(_remark);
            if (remarkBytes.Length > ushort.MaxValue)
                throw new ArgumentException("Remark too long, max " + ushort.MaxValue);

            long contentLength = msg.Content != null ? msg.Content.Length : 0;
            long totalLength = 8 + 1 + 2 + remarkBytes.Length + contentLength;

            // 构建头部
            byte[] header = new byte[11 + remarkBytes.Length];
            Array.Copy(BitConverter.GetBytes(totalLength), 0, header, 0, 8);
            header[8] = _type;
            Array.Copy(BitConverter.GetBytes((ushort)remarkBytes.Length), 0, header, 9, 2);
            Array.Copy(remarkBytes, 0, header, 11, remarkBytes.Length);

            // 缓存策略
            if (totalLength <= 4 * 1024 * 1024)
            {
                using (var ms = new MemoryStream())
                {
                    ms.Write(header, 0, header.Length);
                    if (msg.Content != null)
                        CopyStream.Copy(msg.Content, ms);

                    _buffer = new MemoryBuffer(ms.ToArray());
                }
            }
            else if (totalLength <= 16 * 1024 * 1024)
            {
                var list = new List<byte[]>();
                list.Add(header);

                if (msg.Content != null)
                {
                    byte[] buf = new byte[ChunkSize];
                    int read;
                    while ((read = msg.Content.Read(buf, 0, buf.Length)) > 0)
                    {
                        if (read < buf.Length)
                        {
                            byte[] temp = new byte[read];
                            Array.Copy(buf, temp, read);
                            list.Add(temp);
                        }
                        else
                        {
                            list.Add((byte[])buf.Clone());
                        }
                    }
                }

                _buffer = new ListBuffer(list);
            }
            else
            {
                _buffer = new MemoryMappedBuffer(header, msg.Content);
            }
        }
        /// <summary>
        /// 构造函数重载：直接传入已封包完成的 Stream
        /// </summary>
        public SerializedMessage(Stream serializedStream)
        {
            if (serializedStream == null) throw new ArgumentNullException("serializedStream");
            if (!serializedStream.CanSeek) throw new ArgumentException("Stream must be seekable");

            long totalLength = serializedStream.Length;

            if (totalLength < 11)
                throw new ArgumentException("Serialized stream too short to contain header");

            // 读取 header
            byte[] header = new byte[11];
            serializedStream.Position = 0;
            serializedStream.Read(header, 0, header.Length);

            long declaredLength = BitConverter.ToInt64(header, 0);
            byte _type = header[8];
            ushort remarkLen = BitConverter.ToUInt16(header, 9);
            // 内容起始位置
            long contentStartPos = 11 + remarkLen;
            long contentLength = totalLength - contentStartPos;

            // 缓存策略
            if (totalLength <= 4 * 1024 * 1024)
            {
                byte[] buf = new byte[totalLength];
                serializedStream.Position = 0;
                serializedStream.Read(buf, 0, buf.Length);
                _buffer = new MemoryBuffer(buf);
            }
            else if (totalLength <= 16 * 1024 * 1024)
            {
                var list = new List<byte[]>();
                serializedStream.Position = 0;
                byte[] buf = new byte[ChunkSize];
                int read;
                while ((read = serializedStream.Read(buf, 0, buf.Length)) > 0)
                {
                    if (read < buf.Length)
                    {
                        byte[] temp = new byte[read];
                        Array.Copy(buf, temp, read);
                        list.Add(temp);
                    }
                    else
                    {
                        list.Add((byte[])buf.Clone());
                    }
                }
                _buffer = new ListBuffer(list);
            }
            else
            {
                serializedStream.Position = 0;
                _buffer = new MemoryMappedBuffer(serializedStream);
            }
        }

        /// <summary>
        /// 分块读取 Payload
        /// </summary>
        public void Read(Action<byte[]> callback)
        {
            _buffer.ForEachSegment(callback);
        }

        /// <summary>
        /// 多消费者 Take
        /// </summary>
        public void Take()
        {
            if (_dispatchEnded != 0)
                throw new InvalidOperationException("Dispatch ended");

            Interlocked.Increment(ref _takeCount);
        }

        /// <summary>
        /// 多消费者 End
        /// </summary>
        public void End()
        {
            if (Interlocked.Decrement(ref _takeCount) == 0)
            {
                TryDispose();
            }
        }

        /// <summary>
        /// 分发完成
        /// </summary>
        public void EndDispatch()
        {
            _dispatchEnded = 1;
            TryDispose();
        }

        private void TryDispose()
        {
            if (_dispatchEnded != 0 && _takeCount == 0)
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    _buffer.Dispose();
#if DEBUG
                    Console.WriteLine("[{0}] Dispose.", this.GetType().Name);
#endif
                }
            }
        }

        public void Dispose()
        {
            EndDispatch();
        }
    }

}
