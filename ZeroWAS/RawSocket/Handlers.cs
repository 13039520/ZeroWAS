using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    public class Handlers<TUser>: IRawSocketHandlers<TUser>
    {
        public delegate AuthResult<TUser> ConnectedHandler(IHttpRequest req, string channelPath);
        public delegate void DisconnectedHandler(IRawSocketContext<TUser> context, Exception ex);
        public delegate void ReceivedHandler(IRawSocketContext<TUser> context, IRawSocketData data);

        public ConnectedHandler OnConnectedHandler { get; set; }
        public DisconnectedHandler OnDisconnectedHandler { get; set; }
        public ReceivedHandler OnReceivedHandler { get; set; }
    }
}
