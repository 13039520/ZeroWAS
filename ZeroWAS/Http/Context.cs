using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Http
{
    public class Context: IHttpContext
    {

        private IHttpRequest _Request = null;
        /// <summary>
        /// 客户端请求
        /// </summary>
        public IHttpRequest Request { get { return _Request; } }
        private IHttpResponse _Response = null;
        /// <summary>
        /// 服务端响应
        /// </summary>
        public IHttpResponse Response { get { return _Response; } }
        private IWebApplication _Server = null;
        /// <summary>
        /// 服务端
        /// </summary>
        public IWebApplication Server { get { return _Server; } }

        public object GetService(Type serviceType)
        {
            return Server.GetService(serviceType);
        }

        public Context(IWebApplication server,IHttpRequest request, IHttpResponse response)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }
            this._Response = response;
            this._Request = request;
            this._Server = server;
        }

    }
}
