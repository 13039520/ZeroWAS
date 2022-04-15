using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IRawSocketHandlers<TUser>
    {
        RawSocket.Handlers<TUser>.ConnectedHandler OnConnectedHandler { get; }
        RawSocket.Handlers<TUser>.DisconnectedHandler OnDisconnectedHandler { get; }
        RawSocket.Handlers<TUser>.ReceivedHandler OnReceivedHandler { get; }
    }
}
