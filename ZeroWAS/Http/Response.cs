using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Http
{
    
    public class Response<TUser>: IHttpResponse
    {
        public delegate void EndHandler(IHttpConnection<TUser> client, byte[] responseBytes, Status status, IHttpProcessingResult result, IHttpDataReceiver httpDataReceiver);

        private bool _IsWriteStaticFile = false;
        private bool _IsWriteChunked = false;
        private bool _IsWriteChunkedBeginning = false;
        private bool _IsWriteChunkedEnd = false;
        private bool _IsWriteNormal = false;
        private float _ClinetHttpVersionNum = 1.1F;
        private bool _IsEnd = false;
        bool _HasResponseEndHandler = false;
        private List<byte> _Content = new List<byte>();
        private List<Cookie> _Cookies = new List<Cookie>();
        private System.Collections.Specialized.NameValueCollection _Header = new System.Collections.Specialized.NameValueCollection();
        private EndHandler _OnEndHandler = null;
        private string _RedirectURL = string.Empty;
        private string _ContentType = "text/html";
        private Status _Status = Status.OK;
        private IHttpConnection<TUser> _Client = null;
        private IHttpRequest _Request = null;
        private IWebApplication _HttpServer = null;
        private IHttpDataReceiver _HttpDataReceiver = null;
        private DateTime _BeginTime = DateTime.Now;
        private long _ReturnedContentLength = 0;

        public string ContentType { get { return _ContentType; } set { _ContentType = value; } }
        /// <summary>
        /// 默认值：200 OK
        /// </summary>
        public Status StatusCode { get { return _Status; } set { _Status = value; } }

        public Response(IHttpConnection<TUser> client, IWebApplication httpServer, IHttpRequest httpRequest, IHttpDataReceiver httpDataReceiver, bool hasResponseEndHandler, EndHandler handler)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }
            if (httpRequest == null)
            {
                throw new ArgumentNullException("httpRequest");
            }
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            _HttpServer = httpServer;
            _Client = client;
            _OnEndHandler = handler;
            _Request = httpRequest;
            _HasResponseEndHandler = hasResponseEndHandler;
            _HttpDataReceiver = httpDataReceiver;
            if (httpRequest != null && !string.IsNullOrEmpty(httpRequest.HttpVersion))
            {
                _ClinetHttpVersionNum = Convert.ToSingle(httpRequest.HttpVersion.Split('/')[1]);
            }

        }

        public void AddHeader(string name, string value)
        {
            if (_IsEnd) { return; }

            if (string.IsNullOrEmpty(name)) { return; }
            if (string.IsNullOrEmpty(value)) { return; }
            name = name.Trim().Replace("\r\n", "\r\n");
            string t = name.ToLower();
            if (t == "server" || t == "content-lengt" || t == "connection"|| t == "set-cookie")
            {
                return;
            }
            value = value.Trim().Replace("\r\n", "\r\n");
            _Header.Remove(name);
            _Header.Add(name, value);
        }
        public void AddCookie(Cookie cookie)
        {
            if (_IsEnd) { return; }
            if (cookie == null) { return; }
            Cookie old = _Cookies.Find(o => string.Equals(o.Name, cookie.Name, StringComparison.OrdinalIgnoreCase));
            if (old != null)
            {
                _Cookies.Remove(old);
            }
            _Cookies.Add(cookie);
        }
        public void WriteStaticFile(System.IO.FileInfo file)
        {
            if (_IsWriteChunked)
            {
                throw new Exception("Write Chunked");
            }
            if (_IsWriteNormal)
            {
                throw new Exception("Write Normal");
            }
            if (_IsWriteStaticFile)
            {
                throw new Exception("Only supports one file");
            }
            _IsWriteStaticFile = true;

            this.ContentType = Common.FileMimeTypeMapping.GetMimeType(System.IO.Path.GetExtension(file.FullName));

            if (!file.Exists)
            {
                this.StatusCode = Status.Not_Found;
                this.FireEndHandler();
                return;//文件不存在
            }
            string inputIfModifiedSince = _Request.Header["If-Modified-Since"];
            string lastModified = file.LastWriteTime.ToString("r", System.Globalization.DateTimeFormatInfo.InvariantInfo);
            if (!string.IsNullOrEmpty(inputIfModifiedSince) && inputIfModifiedSince == lastModified)
            {
                _IsWriteStaticFile = false;
                this.StatusCode = Status.Not_Modified;
                this.FireEndHandler();
                return;//文件与客户端的缓存副本一致
            }
            this.AddHeader("Cache-Control", "max-age=300");
            this.AddHeader("Last-Modified", lastModified);

            var range = _Request.Header["Range"];
            if (!string.IsNullOrEmpty(range))
            {
                _IsWriteStaticFile = false;
                WriteRange(file);
                return;
            }

            byte[] buffer;
            System.IO.FileStream fs = null;
            try
            {
                fs = file.OpenRead();
                if (fs.Length > _HttpServer.HttpLargeFileOutputThreshold)//1M
                {
                    if (_ClinetHttpVersionNum != 1.1F)
                    {
                        this.StatusCode = Status.HTTP_Version_Not_Supported;
                        this.FireEndHandler();
                        return;
                    }
                    else
                    {
                        _IsWriteStaticFile = false;
                        this.ContentType = Common.FileMimeTypeMapping.GetMimeType(System.IO.Path.GetExtension(file.FullName));
                        WriteBigStaticFile(fs, _HttpServer.HttpLargeFileOutputRate);//0.1M
                        _ReturnedContentLength = fs.Length;
                        return;
                    }
                }

                buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                fs.Close();
            }
            catch
            {
                if (fs != null)
                {
                    fs.Close();
                }
                buffer = null;
            }
            _IsWriteStaticFile = false;
            if (buffer != null)
            {
                this.Write(buffer);
            }
            else
            {
                this.StatusCode = Status.Expectation_Failed;
            }
            this.FireEndHandler();
        }
        public void WriteChunked(byte[] bytes)
        {
            if (_IsWriteStaticFile)
            {
                throw new Exception("Write Static File");
            }
            if (_IsWriteNormal)
            {
                throw new Exception("Write Normal");
            }
            if (_ClinetHttpVersionNum != 1.1F)
            {
                throw new Exception("There may be a problem with client support");
            }
            _IsWriteChunked = true;

            try
            {
                if (bytes == null) { bytes= new byte[] { }; }
                int len = bytes.Length;
                _ReturnedContentLength += len;
                List<byte> resBytes = new List<byte>();
                if (!_IsWriteChunkedBeginning)
                {
                    _Client.LastHttpInProcess = true;
                    _IsWriteChunkedBeginning = true;
                    string statusRemark = StatusCode.ToString();
                    int statusCode = Convert.ToInt32(StatusCode);
                    statusRemark = statusRemark.Replace("__", "-").Replace("_", " ");
                    string responseLine = string.Format("HTTP/1.1 {0} {1}\r\n", statusCode, statusRemark);
                    resBytes.AddRange(System.Text.Encoding.UTF8.GetBytes(responseLine));
                    resBytes.AddRange(System.Text.Encoding.UTF8.GetBytes(ChunkedHeadBuilder()));
                    resBytes.Add(13);
                    resBytes.Add(10);
                }
                resBytes.AddRange(System.Text.Encoding.UTF8.GetBytes(string.Format("{0}\r\n", len.ToString("X2"))));
                resBytes.AddRange(bytes);
                resBytes.Add(13);
                resBytes.Add(10);
                _Client.LastActivityTime = DateTime.Now;//必须设置，否则，超过一定时间之后服务器端没有查看到活动就会断开连接
                if (len < 1)//0长度
                {
                    resBytes.Add(13);
                    resBytes.Add(10);//输出结束行
                    if (!_Client.Write(resBytes.ToArray()))
                    {
                        throw new Exception("发送异常");
                    }
                    _IsWriteChunkedEnd = true;//已经输出结束行
                    this.End();
                }
                else
                {
                    if (!_Client.Write(resBytes.ToArray()))
                    {
                        throw new Exception("发送异常");
                    }
                }
            }
            catch
            {
                this.End();
            }
        }
        public void Write(byte[] bytes)
        {
            if (_IsWriteChunked)
            {
                throw new Exception("Write Chunked");
            }
            if (_IsWriteStaticFile)
            {
                throw new Exception("Write Static File");
            }
            if (_IsEnd) { return; }
            if (bytes != null && bytes.Length > 0)
            {
                _IsWriteNormal = true;
                _ReturnedContentLength += bytes.Length;
                _Content.AddRange(bytes);
            }
        }
        public void WriteRange(System.IO.FileInfo file)
        {
            var ranges = _Request.Header["Range"];
            if (!string.IsNullOrEmpty(ranges))
            {
                _IsWriteStaticFile = false;
                int n = ranges.IndexOf("bytes=");
                if (n > -1)
                {
                    ranges = ranges.Substring(n + 6);
                }
                else
                {
                    this.StatusCode = Http.Status.Implemented;
                    this.End();
                    return;
                }
            }
            else
            {
                this.StatusCode = Http.Status.Expectation_Failed;
                this.End();
                return;
            }

            System.IO.FileStream fs = null;
            try
            {
                fs = file.OpenRead();
                long fileLength = fs.Length;
                string[] rs = ranges.Split(new char[] { ','}, StringSplitOptions.RemoveEmptyEntries);
                List<RequestRangeFrame> frames = new List<RequestRangeFrame>();
                foreach (string s in rs)
                {
                    RequestRangeFrame frame = GetRequestRangeFrame(fileLength, rs[0]);
                    if (frame == null)
                    {
                        throw new Exception("分解range错误");
                    }
                    if (frame.EndIndex < frame.BeginIndex || frame.EndIndex >= fileLength)
                    {
                        StatusCode = Status.Requested_Range_Not_Satisfiable;
                        throw new Exception("分解range错误");
                    }
                    frames.Add(frame);
                }
                StatusCode = Status.Partial_Content;

                string eTag = "W/\"" + fileLength + "-" + (file.LastWriteTimeUtc.Ticks / 10000000) + "\"";
                this.AddHeader("Cache-Control", "max-age=300");
                this.AddHeader("Last-Modified", file.LastWriteTime.ToString("r", System.Globalization.DateTimeFormatInfo.InvariantInfo));
                this.AddHeader("ETag", eTag);
                this.AddHeader("Accept-Ranges", "bytes");

                string contentType = Common.FileMimeTypeMapping.GetMimeType(System.IO.Path.GetExtension(file.FullName));
                if (frames.Count > 1)//多个片段
                {
                    string boundary = Guid.NewGuid().ToString("n");
                    byte[] boundaryLineBytes = Encoding.UTF8.GetBytes("--" + boundary);
                    byte[] boundaryEndLineBytes = Encoding.UTF8.GetBytes("--" + boundary + "--");
                    byte[] contentTypeLineBytes = Encoding.UTF8.GetBytes("Content-Type:" + contentType);
                    byte[] newlineBytes = new byte[] { 13, 10 };
                    this.ContentType = "multipart/byteranges;boundary=" + boundary;
                    int index = 0;
                    int count = frames.Count;
                    while (index < count)
                    {
                        RequestRangeFrame frame = frames[index];
                        long len = frame.EndIndex - frame.BeginIndex;
                        if (len < 1) { len = 1; }
                        if (len > 2097152)//上限2M
                        {
                            len = 2097152;
                            frame.EndIndex = frame.BeginIndex + len;
                        }
                        byte[] buffer = new byte[len];
                        fs.Position = frame.BeginIndex;
                        fs.Read(buffer, 0, buffer.Length);

                        if (index > 0)
                        {
                            this.Write(newlineBytes);
                        }
                        this.Write(boundaryLineBytes);
                        this.Write(newlineBytes);
                        this.Write(contentTypeLineBytes);
                        this.Write(newlineBytes);
                        this.Write(Encoding.UTF8.GetBytes("Content-Range:bytes " + frame.BeginIndex + "-" + (frame.EndIndex) + "/" + (fileLength)));
                        this.Write(newlineBytes);
                        this.Write(newlineBytes);
                        this.Write(buffer);
                        this.Write(newlineBytes);

                        index++;
                    }
                    this.Write(boundaryEndLineBytes);
                    this.Write(newlineBytes);
                }
                else//一个片段
                {
                    this.ContentType = contentType;

                    RequestRangeFrame frame = frames[0];
                    long len = frame.EndIndex - frame.BeginIndex;
                    if (len < 1) { len = 1; }
                    if (len > 2097152)//上限2M
                    {
                        len = 2097152;
                        frame.EndIndex = frame.BeginIndex + len;
                    }
                    this.AddHeader("Content-Range", "bytes " + frame.BeginIndex + "-" + (frame.EndIndex) + "/" + (fileLength));

                    byte[] buffer = new byte[len];
                    fs.Position = frame.BeginIndex;
                    fs.Read(buffer, 0, buffer.Length);
                    this.Write(buffer);
                }
                fs.Close();
            }
            catch {
                if (fs != null) { fs.Dispose(); }
                if (this.StatusCode == Status.OK)
                {
                    this.StatusCode = Status.Expectation_Failed;
                }
            }
            this.End();
        }
        private RequestRangeFrame GetRequestRangeFrame(long fileLength, string range)
        {
            RequestRangeFrame reval = new RequestRangeFrame { Total = fileLength };
            switch (range)
            {
                case "0-"://全部
                    reval.BeginIndex = 0;
                    reval.EndIndex = fileLength - 1;
                    break;
                case "0-0"://第一个字节
                    reval.BeginIndex = 0;
                    reval.EndIndex = 0;
                    break;
                case "-1"://最后一个字节
                    reval.BeginIndex = fileLength - 1;
                    reval.EndIndex = fileLength - 1;
                    break;
                default://范围
                    if (range.StartsWith("-"))//如：-500,表示最后 500 字节的内容
                    {
                        reval.BeginIndex = fileLength - Convert.ToInt64(range.Substring(1));
                        reval.EndIndex = fileLength - 1;
                    }
                    else if (range.EndsWith("-"))//如：500-,表示从第 500 字节开始到文件结束部分的内容
                    {
                        reval.BeginIndex = Convert.ToInt64(range.Substring(0, range.Length - 1));
                        reval.EndIndex = fileLength - 1;
                    }
                    else
                    {
                        System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(range, @"^(?<start>\d+)-(?<end>\d+)$");
                        if (m.Success)
                        {
                            reval.BeginIndex = Convert.ToInt64(m.Groups["start"].Value);
                            reval.EndIndex = Convert.ToInt64(m.Groups["end"].Value);
                        }
                        else
                        {
                            reval = null;
                        }
                    }
                    break;
            }
            return reval;
        }
        private class RequestRangeFrame
        {
            public long BeginIndex { get; set; }
            public long EndIndex { get; set; }
            public long Total { get; set; }
        }
        public void End()
        {
            FireEndHandler();
        }
        public void Redirect(string url)
        {
            _RedirectURL = string.IsNullOrEmpty(url) ? "/" : url;
            StatusCode = Status.Found;
            FireEndHandler();
        }


        private void WriteBigStaticFile(System.IO.FileStream fs, int size)
        {
            try
            {
                bool isFristTime = true;
                long len = _ReturnedContentLength = fs.Length;
                byte[] buffer = new byte[size];
                int pos = 0;
                string topStr = "";
                while (!_IsEnd && pos < len)
                {
                    fs.Position = pos;

                    if (pos + buffer.Length < len)
                    {
                        fs.Read(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        int n = (int)(len - pos);
                        buffer = new byte[n];
                        fs.Read(buffer, 0, buffer.Length);
                    }
                    pos += buffer.Length;

                    _Client.LastActivityTime = DateTime.Now;//必须设置，否则，超过一定时间之后服务器端没有查看到活动就会断开连接
                    if (isFristTime)
                    {
                        isFristTime = false;
                        string statusRemark = StatusCode.ToString();
                        int statusCode = Convert.ToInt32(StatusCode);
                        statusRemark = statusRemark.Replace("__", "-").Replace("_", " ");
                        List<byte> bytes = new List<byte>();
                        bytes.AddRange(Encoding.UTF8.GetBytes(string.Format("HTTP/1.1 {0} {1}\r\n", statusCode, statusRemark)));
                        bytes.AddRange(Encoding.UTF8.GetBytes(HeadBuilder(len)));
                        string cookieStr = CookieBuilder();
                        if (!string.IsNullOrEmpty(cookieStr))
                        {
                            bytes.AddRange(Encoding.UTF8.GetBytes(cookieStr));
                        }
                        bytes.Add(13);
                        bytes.Add(10);
                        topStr = Encoding.UTF8.GetString(bytes.ToArray());
                        //输出头信息
                        if (!this._Client.Write(bytes.ToArray()))
                        {
                            //throw new Exception("连接断开1");
                            break;
                        }
                    }
                    if (!this._Client.Write(buffer))
                    {
                        //throw new Exception("连接断开2");
                        break;
                    }
                    if (pos < len)
                    {
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                    //this._Client.Write(new byte[] { 10, 13 });
                    int topLen = topStr.Length;
                }
                fs.Close();
            }
            catch{
                if (fs != null)
                {
                    fs.Close();
                }
                //Console.WriteLine(ex.Message);
            }
            this.End();
        }
        private void FireEndHandler()
        {
            if (_IsEnd) { return; }
            if (_IsWriteChunked)
            {
                if (!_IsWriteChunkedEnd)//没有明确传输结束：补充结束符
                {
                    _Client.Write(System.Text.Encoding.UTF8.GetBytes("0\r\n\r\n"));
                }
            }
            if (!string.IsNullOrEmpty(_RedirectURL))
            {
                _Header.Clear();
                _Content.Clear();
                AddHeader("Location", _RedirectURL);
            }

            _IsEnd = true;
            
            string statusRemark = StatusCode.ToString();
            int statusCode = Convert.ToInt32(StatusCode);
            statusRemark = statusRemark.Replace("__", "-").Replace("_", " ");
            string responseLine = string.Format("HTTP/1.1 {0} {1}\r\n", statusCode, statusRemark);
            string headLines = _IsWriteChunked ? ChunkedHeadBuilder() : HeadBuilder(_Content.Count);
            string cookieStr = CookieBuilder();

            ProcessingResult result = null;
            if (_HasResponseEndHandler)
            {
                result = new ProcessingResult
                {
                    ReceivingTs = _Request.ReceivingTs,
                    ReceivedContentLength = _Request.ContentLength,
                    RequestHeader = NvcToString(_Request.Header),
                    ReturnedContentLength = _ReturnedContentLength,
                    ProcessingTs = DateTime.Now - _BeginTime,
                    RequestHttpLine = string.Format("{0} {1} {2}", _Request.Method, _Request.URI.PathAndQuery, _Request.HttpVersion),
                    ResponseHttpLine = responseLine.Trim(),
                    ResponseHeader = headLines
                };
            }

            if (_IsWriteStaticFile)
            {
                _OnEndHandler(_Client, null, StatusCode, result, _HttpDataReceiver);
            }
            else
            {
                List<byte> topBytes = new List<byte>();
                topBytes.AddRange(System.Text.Encoding.UTF8.GetBytes(responseLine));
                topBytes.AddRange(System.Text.Encoding.UTF8.GetBytes(headLines));
                if (!string.IsNullOrEmpty(cookieStr))
                {
                    topBytes.AddRange(System.Text.Encoding.UTF8.GetBytes(cookieStr));
                }
                //空行
                topBytes.Add(13);
                topBytes.Add(10);
                //主体内容
                topBytes.AddRange(_Content);

                _OnEndHandler(_Client, topBytes.ToArray(), StatusCode, result, _HttpDataReceiver);
            }

        }
        private string HeadBuilder(long contentLenght)
        {
            StringBuilder s = new StringBuilder();
            if (string.IsNullOrEmpty(ContentType))
            {
                ContentType = "text/html";
            }
            //s.AppendFormat("Date:{0}\r\n", DateTime.Now.ToString("r", System.Globalization.DateTimeFormatInfo.InvariantInfo));
            s.AppendFormat("Server:{0}\r\n", _HttpServer.ServerName);
            if (_Request.Connection.ToLower().IndexOf("keep-alive") > -1)
            {
                s.AppendFormat("Connection:{0}\r\n", "keep-alive");
            }
            s.AppendFormat("Content-Type:{0}\r\n", ContentType);
            s.AppendFormat("Content-Length:{0}\r\n", contentLenght);
            foreach (string name in _Header.AllKeys)
            {
                s.AppendFormat("{0}:{1}\r\n", name, _Header[name]);
            }
            return s.ToString();
        }
        private string ChunkedHeadBuilder()
        {
            StringBuilder s = new StringBuilder();
            if (string.IsNullOrEmpty(ContentType))
            {
                ContentType = "text/html";
            }
            s.AppendFormat("Server:{0}\r\n", _HttpServer.ServerName);
            if (_Request.Connection.ToLower().IndexOf("keep-alive") > -1)
            {
                s.AppendFormat("Connection:{0}\r\n", "keep-alive:timeout=60");
            }
            //s.AppendFormat("Connection:{0}\r\n", "close");
            s.AppendFormat("Content-Type:{0}\r\n", ContentType);
            s.AppendFormat("Transfer-Encoding:{0}\r\n", "chunked");
            foreach (string name in _Header.AllKeys)
            {
                s.AppendFormat("{0}:{1}\r\n", name, _Header[name]);
            }
            return s.ToString();
        }
        private string CookieBuilder()
        {
            if (_Cookies.Count < 1) { return ""; }
            StringBuilder s = new StringBuilder();
            foreach(Cookie cookie in _Cookies)
            {
                string str = HttpCookieToString(cookie);
                if (string.IsNullOrEmpty(str)) { continue; }
                s.AppendFormat("Set-Cookie:{0}\r\n", str);
            }
            return s.ToString();
        }
        private string HttpCookieToString(Cookie cookie)
        {
            if (cookie == null)
            {
                return "";
            }
            if (string.IsNullOrEmpty(cookie.Name))
            {
                return "";
            }
            if (string.IsNullOrEmpty(cookie.Value))
            {
                cookie.Value = "";
            }
            StringBuilder s = new StringBuilder();
            s.AppendFormat("{0}={1};", cookie.Name, cookie.Value);
            if (cookie.Expires != null)
            {
                DateTime dt = DateTime.Now.Add(cookie.Expires.Value);
                s.AppendFormat("Expires={0};", dt.ToString("r", System.Globalization.DateTimeFormatInfo.InvariantInfo));
            }
            if (!string.IsNullOrEmpty(cookie.Path))
            {
                s.AppendFormat("Path={0};", cookie.Path);
            }
            if (!string.IsNullOrEmpty(cookie.Domain))
            {
                s.AppendFormat("Domain={0};", cookie.Domain);
            }
            if (cookie.HttpOnly)
            {
                s.Append("HttpOnly");
            }
            return s.ToString();
        }
        private string NvcToString(System.Collections.Specialized.NameValueCollection nvc)
        {
            StringBuilder s = new StringBuilder();
            foreach (string name in nvc.AllKeys)
            {
                s.AppendFormat("{0}:{1}\r\n", name, nvc[name]);
            }
            return s.ToString();
        }


    }
}
