using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.WebSocket
{
    public class PushTask<TUser>
    {
        public ContentOpcodeEnum ContentType { get; set; }
        /// <summary>
        /// 当ContentType是Close,Ping,Pong时将被忽略
        /// </summary>
        public byte[] Content { get; set; }
        public IHttpConnection<TUser> Accepter { get; set; }

    }
}
