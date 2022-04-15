using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace ZeroWAS
{
    public interface IHttpRequest
    {
        TimeSpan ReceivingTs { get; }
        string RequestLine { get; }
        string Method { get; }
        Uri URI { get; }
        string HttpVersion { get; }
        string UserAgent { get; }
        string UserRemoteAddress { get; }
        string UserRemotePort { get; }
        long ContentLength { get; }
        string ContentType { get; }
        string Connection { get; }
        System.IO.Stream InputStream { get; }
        Encoding Encoding { get; }
        NameValueCollection Header { get; }
        NameValueCollection QueryString { get; }
        NameValueCollection Form { get; }
        NameValueCollection Cookies { get; }
        Dictionary<string, List<Http.UploadFile>> Files { get; }
    }
}
