using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.WebSocket
{
    public class AuthResult<TUser>
    {
        public bool IsOk { get; set; }
        public TUser User { get; set; }
        public string WriteMsg { get; set; }
    }
}
