using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.WebSocket
{
    public class PushTask<TUser>
    {
        public IWebSocketDataFrame Frame { get; set; }
        public IHttpConnection<TUser> Accepter { get; set; }

    }
}
