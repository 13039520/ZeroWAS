using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    public class PushTask<TUser>
    {
        public IRawSocketData Content { get; set; }
        public IHttpConnection<TUser> Accepter { get; set; }

    }
}
