using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.App.HttpHandlers
{
    internal class SuffixHttpHandler : ZeroWAS.Http.HttpHeadler
    {
        private Action<ZeroWAS.IHttpContext> callback;
        public SuffixHttpHandler(string handlerKey, string[] suffixes, Action<ZeroWAS.IHttpContext> callback)
            : base(handlerKey, suffixes)
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
