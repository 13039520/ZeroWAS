using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    /// <summary>
    /// 收发数据包
    /// </summary>
    public interface IRawSocketData
    {
        /// <summary>
        /// 唯一编号
        /// </summary>
        long Id { get; }
        /// <summary>
        /// 类型
        /// </summary>
        byte Type { get; }
        /// <summary>
        /// 帧编号
        /// </summary>
        short FrameNum { get;}
        /// <summary>
        /// 帧总量
        /// </summary>
        short FrameTotal { get;}
        /// <summary>
        /// 文件名长度(在Type不是文件类型时忽略，Type是文件类型时在第一帧的内容前附加文件名的字节数据)
        /// </summary>
        short FileNameLength { get;}
        /// <summary>
        /// 长度限制：4M
        /// </summary>
        byte[] Content { get;}
    }

}
