using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.App.HttpHandlers
{
    internal class RegexHttpHandler : ZeroWAS.Http.HttpHeadler
    {
        private Action<ZeroWAS.IHttpContext> callback;
        public RegexHttpHandler(string handlerKey, string pathAndQueryPattern, Action<ZeroWAS.IHttpContext> callback)
            : base(handlerKey, pathAndQueryPattern)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            this.callback = callback;
        }

        public override void ProcessRequest(ZeroWAS.IHttpContext context)
        {
            if (callback != null)
            {
                callback(context);
            }
            else
            {
                base.ProcessRequest(context);
            }
        }
    }
}
