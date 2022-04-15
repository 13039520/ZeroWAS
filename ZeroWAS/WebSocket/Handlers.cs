using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.WebSocket
{
    public class Handlers<TUser>: IWebSocketHandlers<TUser>
    {
        public delegate AuthResult<TUser> ConnectedHandler(IHttpRequest req, string wsChannelPath);
        public delegate void DisconnectedHandler(IWebSocketContext<TUser> context, Exception ex);
        public delegate void TextFrameReceivedHandler(IWebSocketContext<TUser> context, string text);
        public delegate void ContinuationFrameReceivedHandler(IWebSocketContext<TUser> context, byte[] content, bool FIN);
        public delegate void BinaryFrameReceivedHandler(IWebSocketContext<TUser> context, byte[] content);

        public ConnectedHandler OnConnectedHandler { get; set; }
        public DisconnectedHandler OnDisconnectedHandler { get; set; }
        public TextFrameReceivedHandler OnTextFrameReceivedHandler { get; set; }
        public ContinuationFrameReceivedHandler OnContinuationFrameReceivedHandler { get; set; }
        public BinaryFrameReceivedHandler OnBinaryFrameReceivedHandler { get; set; }
    }
}
