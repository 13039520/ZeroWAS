using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IHttpContext
    {
        IHttpRequest Request { get; }
        IHttpResponse Response { get; }
        IWebApplication Server { get; }
        object GetService(Type serviceType);
    }
}
