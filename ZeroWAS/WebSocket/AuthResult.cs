using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.WebSocket
{
    public class AuthResult<TUser>
    {
        public bool IsOk { get; set; }
        public TUser User { get; set; }
        private ContentOpcodeEnum _ContentOpcode = ContentOpcodeEnum.Text;
        public ContentOpcodeEnum ContentOpcode { get { return _ContentOpcode; } set { _ContentOpcode = value; } }
        public byte[] Content { get; set; }
    }
}
