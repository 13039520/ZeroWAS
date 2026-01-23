using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace ZeroWAS
{
    public interface IHttpRequest: IDisposable
    {
        TimeSpan ReceivingTs { get; }
        string RequestLine { get; }
        string Method { get; }
        Uri URI { get; }
        string HttpVersion { get; }
        /// <summary>
        /// HTTP 版本号的数字值(0.9=9,1.0=10,1.1=11,2.0=20,3.0=30)
        /// </summary>
        int HttpVersionNumber { get; }
        /// <summary>
        /// 如果客户端没有传入且http是1.0或更低时默认：unknown.org
        /// </summary>
        string HostName {  get; }
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
