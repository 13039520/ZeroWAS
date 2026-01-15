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
        /// <summary>
        /// 临时缓存文件路径(用于对象被清理时删除缓存文件)
        /// </summary>
        public string TempChaceFileFullPath { get; set; }

        private int _disposed;
        public void Dispose()
        {
            // 原子性防止重复 Dispose
            if (System.Threading.Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            // 1. 释放流
            var stream = InputStream;
            InputStream = null;
            if (stream != null)
            {
                try
                {
                    stream.Dispose();
                }
                catch
                {
                    // 日志即可，不要抛
                }
            }

            // 2. 删除临时文件
            var path = TempChaceFileFullPath;
            TempChaceFileFullPath = null;
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }
                catch
                {
                    // 文件被占用 / IO 错误，允许失败
                }
                //Console.WriteLine("删除临时文件=>"+path);
            }
        }

    }


}
