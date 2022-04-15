using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IHttpResponse
    {
        string ContentType { get; set; }
        Http.Status StatusCode { get; set; }
        void AddHeader(string name, string value);
        void AddCookie(Http.Cookie cookie);
        void Redirect(string url);
        /// <summary>
        /// Files over LargeFileOutputThreshold will be transferred in "chunked" mode (the "WriteChunked" method is actually called)
        /// </summary>
        /// <param name="fileInfo"></param>
        void WriteStaticFile(System.IO.FileInfo fileInfo);
        /// <summary>
        /// Transfer-Encoding:chunked
        /// </summary>
        /// <param name="bytes"></param>
        void WriteChunked(byte[] bytes);
        void WriteRange(System.IO.FileInfo file);
        void Write(byte[] bytes);
        void End();
    }
}
