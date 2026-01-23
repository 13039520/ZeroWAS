using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    internal class PushTask<TUser>: IRawSocketPushTask<TUser>
    {
        public IRawSocketSerializedMessage Content { get; set; }
        public List<IHttpConnection<TUser>> Accepters { get; set; }

    }
}
