using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZeroWAS
{
    /// <summary>
    /// 发送端消息接口
    /// </summary>
    public interface IRawSocketSendMessage: IDisposable
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        byte Type { get; }
        /// <summary>
        /// 主体内容
        /// </summary>
        Stream Content { get; }
        /// <summary>
        /// 备注信息
        /// </summary>
        string Remark { get; }
    }
}
