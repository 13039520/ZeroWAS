using System;
using System.Collections.Generic;
using System.Text;
namespace ZeroWAS
{
    /// <summary>
    /// 序列化消息接口
    /// </summary>
    public interface IRawSocketSerializedMessage : IDisposable
    {
        void Read(Action<byte[]> callback);
        void Take();
        void End();
        //void EndDispatch();
    }
}
