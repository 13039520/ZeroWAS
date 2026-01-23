using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZeroWAS
{
    /// <summary>
    /// 接收端消息接口
    /// </summary>
    public interface IRawSocketReceivedMessage : IDisposable
    {
        /// <summary>消息类型</summary>
        byte Type { get; }

        /// <summary>消息备注内容</summary>
        string Remark { get; }
        /// <summary>
        /// 消息备注内容长度(字节数)
        /// </summary>
        ushort RemarkLength {  get; }
        /// <summary>
        /// 主体内容长度(字节数)
        /// </summary>
        long ContentLength {  get; }
        /// <summary>
        /// 分块读取消息内容
        /// </summary>
        void ReadContent(Action<byte[]> callback);
        /// <summary>
        /// 读取消息内容为字符串
        /// </summary>
        string ReadContentAsString(Encoding encoding);
        /// <summary>
        /// 复制原始封包数据到指定 Stream
        /// </summary>
        void CopyAllData(Stream stream);
        /// <summary>
        /// 复制主体内容到指定 Stream
        /// </summary>
        /// <param name="stream"></param>
        void CopyContentData(Stream stream);
    }
}
