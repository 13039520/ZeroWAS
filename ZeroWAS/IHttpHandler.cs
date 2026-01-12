using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ZeroWAS
{
    public interface IHttpHandler
    {
        string Key { get; }
        string PathAndQueryPattern { get; }
        System.Text.RegularExpressions.RegexOptions RegexOptions {  get; }
        void ProcessRequest(IHttpContext context);
    }
}
