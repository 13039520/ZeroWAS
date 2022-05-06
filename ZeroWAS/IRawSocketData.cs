using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    /// <summary>
    /// 收发数据包
    /// </summary>
    public interface IRawSocketData : IDisposable
    {
        Guid Handler { get; }
        event RawSocket.DataFrameDisposedHandler OnDisposed;
        byte FrameType { get; }
        string FrameRemark { get; }
        System.IO.Stream FrameContent { get; }
        void ReadAll(RawSocket.DataFrameReadHandler handler);
        void ReadContent(RawSocket.DataFrameReadHandler handler);
        string GetFrameContentString();
    }

}
