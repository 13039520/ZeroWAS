using System;
using System.Collections.Generic;
using System.Text;
namespace ZeroWAS
{
    /// <summary>
    /// RawSocket 待推送消息
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IRawSocketPushTask<TUser>
    {
        IRawSocketSerializedMessage Content { get; }
        List<IHttpConnection<TUser>> Accepters { get; }
    }
}
