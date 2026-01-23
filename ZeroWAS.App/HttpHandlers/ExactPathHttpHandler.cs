using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.App.HttpHandlers
{
    internal class ExactPathHttpHandler : ZeroWAS.Http.HttpHeadler
    {
        private Action<ZeroWAS.IHttpContext> callback;
        public ExactPathHttpHandler(string handlerKey, string pathAndQuery, Action<ZeroWAS.IHttpContext> callback)
            : base(handlerKey, pathAndQuery, false)
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
