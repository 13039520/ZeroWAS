using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    public class AuthResult<TUser>
    {
        public bool IsOk { get; set; }
        public TUser User { get; set; }
        public byte MessageType { get; set; }
        public byte[] MessageContent { get; set; }
        public string MessageRemark { get; set; }
    }
}
