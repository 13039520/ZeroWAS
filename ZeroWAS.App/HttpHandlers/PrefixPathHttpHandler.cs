using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.App.HttpHandlers
{
    internal class PrefixPathHttpHandler : ZeroWAS.Http.HttpHeadler
    {
        private Action<ZeroWAS.IHttpContext> callback;
        public PrefixPathHttpHandler(string handlerKey, string pathAndQuery, Action<ZeroWAS.IHttpContext> callback)
            : base(handlerKey, pathAndQuery, true)
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
