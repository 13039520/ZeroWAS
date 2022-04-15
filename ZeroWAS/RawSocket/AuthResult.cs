using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    public class AuthResult<TUser>
    {
        public bool IsOk { get; set; }
        public TUser User { get; set; }
        public string WriteData { get; set; }
    }
}
