using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZeroWAS.Common;

namespace ZeroWAS.RawSocket
{
    internal class MessageReceiver
    {
        private const int LengthFieldSize = 8; // long
        private const long FileThreshold = 4L * 1024 * 1024; // 4MB
        private MemoryStream _buffer = new MemoryStream();
        private long _expectedLength = -1;
        private Stream _dataStream;
        private string _dataFilePath=string.Empty;

        /// <summary>
        /// 当完整包准备好时触发回调
        /// </summary>
        public event Action<ReceivedMessage> OnMessage;

        /// <summary>
        /// 接收字节流
        /// </summary>
        public void Receive(byte[] data)
        {
            Receive(data, 0, data.Length);
        }

        public void Receive(byte[] data, int offset, int count)
        {
            if (_expectedLength < 1)
            {
                _buffer.Position = _buffer.Length;
                _buffer.Write(data, offset, count);
            }
            else
            {
                WriteBody(data, offset, count);
            }
            ProcessBuffer();
        }
        private void WriteBody(byte[] data, int offset, int count)
        {
            //已经缓存的长度
            long written = _dataStream.Length;
            //剩余的缓存长度
            long remain = _expectedLength - written;
            //需要写入的缓存长度
            int toWrite = remain >= count ? count : (int)remain;

            _dataStream.Write(data, offset, toWrite);

            if (toWrite == remain)
            {
                if (count > toWrite)
                {
                    _buffer.Position = _buffer.Length;
                    _buffer.Write(data, offset + toWrite, count - toWrite);
                }
                _dataStream.Position = 0;
                if(OnMessage != null)
                {
                    //由 ReceivedMessage 来释放资源和删除可能存在的缓存文件
                    using (var msg = new ReceivedMessage(_dataStream, _dataFilePath))
                    {
                        OnMessage?.Invoke(msg);
                    }
                }
                else
                {
                    _dataStream?.Dispose();
                    _dataFilePath = string.Empty;
                }
                _expectedLength = -1;
            }
        }
        /// <summary>
        /// 初始化 _dataStream (必须在确定数据包长度之后)
        /// </summary>
        private void DataStreamInitialize()
        {
            if (_dataStream != null) { _dataStream.Dispose(); _dataStream = null; }
            if(_expectedLength > FileThreshold)
            {
                _dataFilePath = TempFile.GetTempFileName("rsr-");
                _dataStream = new FileStream(_dataFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            }
            else
            {
                _dataFilePath = string.Empty;
                _dataStream = new MemoryStream();
            }
            if (_buffer.Length > 0)
            {
                byte[] temp = _buffer.ToArray();
                _buffer.SetLength(0);
                // 清空 _buffer 并将缓存内容写入 _dataStream
                WriteBody(temp, 0, temp.Length);
            }
        }
        private void ProcessBuffer()
        {
            if (_expectedLength < 0)
            {
                if (_buffer.Length < LengthFieldSize)
                    return;

                _buffer.Position = 0;
                _expectedLength = ReadInt64(_buffer);
                if (_expectedLength <= 0)
                {
                    throw new InvalidOperationException("Invalid packet length");
                }
                DataStreamInitialize();
            }
        }

        #region 工具方法
        private static long ReadInt64(Stream stream)
        {
            byte[] buf = new byte[8];
            int read = 0;
            while (read < 8)
            {
                int r = stream.Read(buf, read, 8 - read);
                if (r <= 0) throw new EndOfStreamException();
                read += r;
            }
            return BitConverter.ToInt64(buf, 0);
        }
        #endregion
    }
}
