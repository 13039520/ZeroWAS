using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace ZeroWAS.Http
{
    public class Request : IHttpRequest
    {
        System.Text.Encoding _Encoding = System.Text.Encoding.UTF8;

        public TimeSpan ReceivingTs { get; set; }
        public string RequestLine { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
        public Uri URI { get; set; }
        public string HttpVersion { get; set; }
        public string UserAgent { get; set; }
        public string UserRemoteAddress { get; set; }
        public string UserRemotePort { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public string Connection { get; set; }
        public System.IO.Stream InputStream { get; set; }
        public System.Text.Encoding Encoding { get { return _Encoding; } set { _Encoding = value; } }
        public NameValueCollection Header { get; set; }
        public NameValueCollection QueryString { get; set; }
        public NameValueCollection Form { get; set; }
        public NameValueCollection Cookies { get; set; }
        public Dictionary<string, List<Http.UploadFile>> Files { get; set; }


    }


}
