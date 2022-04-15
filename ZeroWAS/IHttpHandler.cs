using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IHttpHandler
    {
        string Key { get; }
        string[] Suffix { get; }
        string[] BasePath { get; }

        void ProcessRequest(IHttpContext context);
    }
}
