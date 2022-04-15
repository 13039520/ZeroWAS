using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Http
{
    public class Handlers
    {
        public delegate void RequestReceivedHandler(IHttpContext context);
        public delegate void ResponseEndHandler(IHttpProcessingResult result);
        public delegate void RawStreamReceivedHandler<TUser>(IHttpConnection<TUser> httpSocket, IHttpDataReceiver receiver, byte[] bytes);
        public delegate void ErrorHandler(object sender, Exception ex);
    }
}
