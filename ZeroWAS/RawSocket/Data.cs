using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    /// <summary>
    /// 收发数据包
    /// </summary>
    public class Data: IRawSocketData
    {
        /// <summary>
        /// 消息编号
        /// </summary>
        public long Id { get; set; }
        private byte _Type = 1;
        /// <summary>
        /// 消息类型(默认文本类型)：
        /// <para>1 负载文本类内容</para>
        /// <para>2 负载文件类内容</para>
        /// <para>101 单个数据包接收成功确认</para>
        /// <para>102 单个文件内容接收成功确认</para>
        /// </summary>
        public byte Type { get { return _Type; } set { _Type = value; } }
        /// <summary>
        /// 帧编号
        /// </summary>
        public short FrameNum { get; set; }
        /// <summary>
        /// 帧总数
        /// </summary>
        public short FrameTotal { get; set; }
        /// <summary>
        /// 文件名称长度（文件名，可以前缀"/"以携带文件夹名称，如"/a/123.txt"，将会在服务器端接收文件的根文件夹中创建一个名为"a"的文件夹）
        /// <para>备注：当一个文件被分成多个片段进行发送时只需要第一个片段携带文件名称即可</para>
        /// </summary>
        public short FileNameLength { get; set; }
        /// <summary>
        /// 消息内容(原则上只读，允许最大负载4M)
        /// </summary>
        public byte[] Content { get; set; }
    }

}
