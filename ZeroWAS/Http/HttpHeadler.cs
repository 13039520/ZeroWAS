using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ZeroWAS.Http
{
    public abstract class HttpHeadler : IHttpHandler
    {
        private string _Key = "";
        public string Key { get { return _Key; } }
        public System.Text.RegularExpressions.Regex CompiledRegex { get; }

        public HttpHeadler(string handlerKey, string pathAndQueryPattern, System.Text.RegularExpressions.RegexOptions regexOptions= System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        {
            if (!string.IsNullOrEmpty(handlerKey))
            {
                _Key = handlerKey;
            }
            if (string.IsNullOrEmpty(pathAndQueryPattern))
            {
                throw new ArgumentException(nameof(pathAndQueryPattern));
            }
            CompiledRegex= new System.Text.RegularExpressions.Regex(pathAndQueryPattern, regexOptions|System.Text.RegularExpressions.RegexOptions.Compiled);
        }

        public virtual void ProcessRequest(IHttpContext context)
        {
            System.IO.FileInfo fileInfo = context.Server.GetStaticFile(context.Request.URI.AbsolutePath);
            if (fileInfo != null)
            {
                context.Response.WriteStaticFile(fileInfo);
            }
            else
            {
                context.Response.StatusCode = Status.Not_Found;
            }
            context.Response.End();
        }
    }
}
