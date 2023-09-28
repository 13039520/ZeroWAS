using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IWebSocketDataFrame
    {
        byte[] GetBytes();
        byte[] Content { get; }
        string Text { get; }
        WebSocket.DataFrameHeader Header { get; }
        byte[] Payload { get; }
    }



}
