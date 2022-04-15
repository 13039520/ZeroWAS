using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IWebSocketHandlers<TUser>
    {
        WebSocket.Handlers<TUser>.ConnectedHandler OnConnectedHandler { get; }
        WebSocket.Handlers<TUser>.DisconnectedHandler OnDisconnectedHandler { get; }
        WebSocket.Handlers<TUser>.TextFrameReceivedHandler OnTextFrameReceivedHandler { get; }
        WebSocket.Handlers<TUser>.ContinuationFrameReceivedHandler OnContinuationFrameReceivedHandler { get; }
        WebSocket.Handlers<TUser>.BinaryFrameReceivedHandler OnBinaryFrameReceivedHandler { get; }
    }
}
