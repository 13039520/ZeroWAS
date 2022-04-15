using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public delegate void HttpSocketDisposedHandler<TUser>(System.Exception ex);
    public interface IHttpConnection<TUser> : IDisposable
    {
        event HttpSocketDisposedHandler<TUser> OnDisposed;
        Http.Handlers.ErrorHandler OnErrorHandler { get; set; }
        Http.Handlers.RawStreamReceivedHandler<TUser> OnRawStreamReceivedHandler { get; set; }
        bool IsHttps { get; }
        string WebSocketChannelPath { get; set; }
        string RawSocketChannelPath { get; set; }
        TUser User { get; set; }
        long ClinetId { get; set; }
        int HttpRequestCount { get; set; }
        bool IsDataMasked { get; set; }
        string RemoteEndPointStr { get; set; }
        bool LastHttpInProcess { get; set; }
        Common.SocketTypeEnum SocketType { get; set; }
        DateTime LastActivityTime { get; set; }

        bool Write(byte[] bytes);

    }
}
