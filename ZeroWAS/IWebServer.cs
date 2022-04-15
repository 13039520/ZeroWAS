using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IWebServer<TUser>: IDisposable
    {
        IWebApplication WebApp { get; }
        IWebSocketHub<TUser> WebSocketHub { get; }
        IRawSocketHub<TUser> RawSocketHub { get; }

        void ListenStart();
        void AddHttpHandler(IHttpHandler handler);

    }
}
