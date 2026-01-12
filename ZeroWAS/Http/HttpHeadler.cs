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
        private string _PathAndQueryPattern = "";
        public string PathAndQueryPattern { get { return _PathAndQueryPattern; } }
        private System.Text.RegularExpressions.RegexOptions _RegexOptions;
        public System.Text.RegularExpressions.RegexOptions RegexOptions { get { return _RegexOptions; } }

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
            _RegexOptions= regexOptions;
            _PathAndQueryPattern = pathAndQueryPattern;
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
