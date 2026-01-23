using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZeroWAS.RawSocket
{
    /// <summary>
    /// 接收端消息实现
    /// </summary>
    public sealed class ReceivedMessage : IRawSocketReceivedMessage
    {
        private readonly Stream _serializedStream;
        private readonly string _tempFile;
        private readonly long _contentStartPosition;
        private readonly byte _type;
        private readonly string _remark;
        private readonly long _contentLength;
        private readonly ushort _remarkLength;

        public byte Type => _type;
        public string Remark => _remark;
        public ushort RemarkLength => _remarkLength;
        public long ContentLength => _contentLength;

        /// <summary>
        /// 构造函数：接收完整封包 Stream，可选指定临时文件路径
        /// </summary>
        public ReceivedMessage(Stream stream, string tempFilePath = "")
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (!stream.CanSeek) throw new ArgumentException("Stream must be seekable");

            _serializedStream = stream;
            _tempFile = tempFilePath ?? "";

            // 解析封包头部
            _serializedStream.Position = 0;
            byte[] header = new byte[11];
            _serializedStream.Read(header, 0, header.Length);

            long totalLength = BitConverter.ToInt64(header, 0);
            _type = header[8];
            ushort remarkLen = BitConverter.ToUInt16(header, 9);

            byte[] remarkBytes = new byte[remarkLen];
            if (remarkLen > 0)
            {
                _serializedStream.Read(remarkBytes, 0, remarkBytes.Length);
                _remark = Encoding.UTF8.GetString(remarkBytes);
            }
            else
            {
                _remark = string.Empty;
            }

            _contentStartPosition = 11 + remarkLen;
            _remarkLength = remarkLen;
            _contentLength = totalLength - 11 - remarkLen;
        }

        public void ReadContent(Action<byte[]> callback)
        {
            if (callback == null) throw new ArgumentNullException("callback");

            const int ChunkSize = 2048;
            byte[] buffer = new byte[ChunkSize];

            lock (_serializedStream)
            {
                _serializedStream.Position = _contentStartPosition;
                int read;
                while ((read = _serializedStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (read < buffer.Length)
                    {
                        byte[] temp = new byte[read];
                        Array.Copy(buffer, temp, read);
                        callback(temp);
                    }
                    else
                    {
                        callback((byte[])buffer.Clone());
                    }
                }
            }
        }
        public string ReadContentAsString(Encoding encoding)
        {
            if (encoding == null) encoding = Encoding.UTF8;

            using (MemoryStream ms = new MemoryStream())
            {
                ReadContent(bytes => ms.Write(bytes, 0, bytes.Length));
                return encoding.GetString(ms.ToArray());
            }
        }
        public void CopyAllData(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            const int ChunkSize = 2048;
            byte[] buffer = new byte[ChunkSize];

            lock (_serializedStream)
            {
                _serializedStream.Position = 0;
                int read;
                while ((read = _serializedStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, read);
                }
            }
        }
        public void CopyContentData(Stream stream)
        {
            ReadContent((bytes) => {
                stream.Write(bytes, 0, bytes.Length);
            });
        }
        public void Dispose()
        {
            _serializedStream.Dispose();

            if (!string.IsNullOrEmpty(_tempFile) && File.Exists(_tempFile))
            {
                try { 
                    File.Delete(_tempFile);
#if DEBUG
                    Console.WriteLine("[{0}]Delete=>{1}", this.GetType().Name, _tempFile);
#endif
                }
                catch { }
            }
        }
    }
}
