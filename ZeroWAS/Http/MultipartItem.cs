using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace ZeroWAS.Http
{
    public sealed class MultipartItem
    {
        public Dictionary<string, string> Headers =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string Name;
        public bool HasFileName;
        public string FileName;
        public string ContentType;

        /// <summary>
        /// 数据在源流中的起始位置
        /// </summary>
        public long DataOffset;

        /// <summary>
        /// 数据长度
        /// </summary>
        public long DataLength;

        /// <summary>
        /// 原始请求体流
        /// </summary>
        internal Stream SourceStream;

        /// <summary>
        /// 将该 part 的数据拷贝到目标流（零拷贝）
        /// </summary>
        public void CopyDataTo(Stream target)
        {
            if (DataLength <= 0)
                return;

            SourceStream.Position = DataOffset;

            byte[] buffer = new byte[8192];
            long remain = DataLength;

            while (remain > 0)
            {
                int read = SourceStream.Read(
                    buffer,
                    0,
                    remain > buffer.Length ? buffer.Length : (int)remain);

                if (read <= 0)
                    break;

                target.Write(buffer, 0, read);
                remain -= read;
            }
        }

        /// <summary>
        /// 按需读取为字符串（普通字段）
        /// </summary>
        public string ReadAsString(Encoding encoding)
        {
            if (DataLength <= 0)
                return string.Empty;

            SourceStream.Position = DataOffset;

            byte[] buf = new byte[DataLength];
            SourceStream.Read(buf, 0, buf.Length);

            return encoding.GetString(buf);
        }
    }
}
