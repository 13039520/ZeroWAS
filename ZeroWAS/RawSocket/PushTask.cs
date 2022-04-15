using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    public class PushTask<TUser>
    {
        /// <summary>
        /// 下限21B,上限4M
        /// </summary>
        public byte[] Content { get; set; }
        public IHttpConnection<TUser> Accepter { get; set; }

    }
}
