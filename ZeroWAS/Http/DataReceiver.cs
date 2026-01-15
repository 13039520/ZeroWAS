using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace ZeroWAS.Http
{
    public class DataReceiver<TUser>: IHttpDataReceiver
    {
        List<byte> bytes = new List<byte>();
        string userRemoteAddress = string.Empty;
        string userRemotePort = string.Empty;
        Request request = null;
        IWebApplication httpServer = null;
        Status receiveErrorHttpStatus = Status.Continue;
        /// <summary>
        /// 请求数据读取状态
        /// <para>0 Request line</para>
        /// <para>1 Request headers</para>
        /// <para>2 Request body</para>
        /// <para>3 Request end</para>
        /// <para>4 Request error</para>
        /// </summary>
        int requestReadStep = 0;
        long readContentLength = 0;
        bool isFormDataWriteToFile = false;
        readonly int formDataWriteFileLimit = 4194304;//4M
        readonly byte[] newlineCharBytes = new byte[] { 13, 10 };
        DateTime receivingTime = DateTime.Now;
        int receivingCount = 0;
        /// <summary>
        /// 文本字段长度限制(8M)
        /// </summary>
        const int TextFieldLengthLimit = 8 * 1024 * 1024;
        /// <summary>
        /// 字段名长度限制(64)
        /// </summary>
        const int FieldNameLengthLimit = 64;

        public Request RequestData { get { return request; } set { request = value; } }
        public Status ReceiveErrorHttpStatus { get { return receiveErrorHttpStatus; } }
        public string ReceiveErrorMsg { get; set; }

        public DataReceiver(string userRemoteAddress,string userRemotePort, IWebApplication httpServer)
        {
            this.userRemoteAddress = userRemoteAddress;
            this.userRemotePort = userRemotePort;
            this.httpServer = httpServer;
        }

        public bool Receive(byte[] myBytes)
        {
            if (requestReadStep < 1)
            {
                receivingTime = DateTime.Now;
                ReceiveErrorMsg = string.Empty;
            }
            bytes.AddRange(myBytes);
            bool isBreak = false;
            while (true)
            {
                
                switch (requestReadStep)
                {
                    case 0:

                        #region -- 待读取请求行 --
                        byte[] bytes1 = bytes.ToArray();
                        int index1 = (int)ByteArrayIndexOf(bytes1, newlineCharBytes, 0);
                        if (index1 > 5)
                        {
                            string requestLine = Encoding.UTF8.GetString(bytes1, 0, index1);
                            if (!RequestLineAnalysis(requestLine))
                            {
                                receiveErrorHttpStatus = Status.Request__URI_Too_Long;
                                requestReadStep = 4;
                                continue;
                            }
                            //移除请求行+回车+换行
                            bytes.RemoveRange(0, index1 + newlineCharBytes.Length);
                            //开始读取请求头部
                            requestReadStep = 1;
                            continue;
                        }
                        else
                        {
                            if (bytes1.Length > httpServer.HttpMaxURILength + 20)
                            {
                                requestReadStep = 4;
                                continue;
                            }
                        }
                        #endregion

                        break;
                    case 1:

                        #region -- 待读取请求头部 --
                        byte[] bytes2 = bytes.ToArray();
                        byte[] doubleNewlineCharBytes = new byte[] { newlineCharBytes[0], newlineCharBytes[1], newlineCharBytes[0], newlineCharBytes[1] };
                        int index2 = (int)ByteArrayIndexOf(bytes2, doubleNewlineCharBytes, 0);
                        if (index2 > 1)
                        {
                            string headerLines = Encoding.UTF8.GetString(bytes2, 0, index2);
                            if (!HeaderAnalysis(headerLines))
                            {
                                requestReadStep = 4;
                                ReceiveErrorMsg = "HTTP头接收异常";
                                continue;
                            }
                            //移除请求头部
                            bytes.RemoveRange(0, index2 + doubleNewlineCharBytes.Length);

                            if (request.ContentLength < 1)
                            {
                                requestReadStep = 3;//读取完成
                                continue;
                            }

                            if (request.ContentLength > httpServer.HttpMaxContentLength)
                            {
                                receiveErrorHttpStatus = Status.Request_Entity_Too_Large;
                                requestReadStep = 4;
                                continue;
                            }
                            if (request.ContentLength > formDataWriteFileLimit)
                            {
                                string dir = System.IO.Path.GetTempPath();
                                string file = System.IO.Path.Combine(dir, "zerowas_" + Guid.NewGuid().ToString("N") + ".tmp");
                                request.TempChaceFileFullPath = file;
                                isFormDataWriteToFile = true;
                                request.InputStream = new System.IO.FileStream(file, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read);
                            }
                            else
                            {
                                isFormDataWriteToFile = false;
                                request.InputStream = new System.IO.MemoryStream();
                            }
                            requestReadStep = 2;//待读取主体请求数据
                            continue;
                        }
                        else
                        {
                            //请求头部超长：100kb
                            if (bytes2.Length > 102400)
                            {
                                requestReadStep = -1;
                                ReceiveErrorMsg = "HTTP头部超长";
                                continue;
                            }
                            else
                            {
                                isBreak = true;
                            }
                        }
                        #endregion 

                        break;
                    case 2:

                        #region -- 待读取请求主体数据(非POST请求将跳过) --
                        int count = bytes.Count;
                        long needCount = request.ContentLength - readContentLength;
                        //接收到的字节不足
                        if (count < needCount)
                        {
                            readContentLength += count;
                            byte[] inputBytes = bytes.ToArray();
                            bytes.Clear();
                            request.InputStream.Write(inputBytes, 0, inputBytes.Length);
                            isBreak = true;
                        }
                        //接收到的字节超出内容所需
                        else if (count > needCount)
                        {
                            readContentLength = request.ContentLength;

                            byte[] inputBytes = new byte[needCount];
                            bytes.CopyTo(0, inputBytes, 0, inputBytes.Length);
                            bytes.RemoveRange(0, inputBytes.Length);
                            request.InputStream.Write(inputBytes, 0, inputBytes.Length);

                            if (isFormDataWriteToFile)
                            {
                                request.InputStream.Close();
                                //只读
                                request.InputStream = new System.IO.FileStream(request.TempChaceFileFullPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
                            }
                            else
                            {
                                byte[] buffer = (request.InputStream as System.IO.MemoryStream).ToArray();
                                request.InputStream.Close();
                                //只读
                                request.InputStream = new System.IO.MemoryStream(buffer, false);
                            }

                            requestReadStep = 3;
                            continue;
                        }
                        //刚好满足需要
                        else
                        {
                            byte[] inputBytes = bytes.ToArray();
                            bytes.RemoveRange(0, inputBytes.Length);
                            request.InputStream.Write(inputBytes, 0, inputBytes.Length);
                            if (isFormDataWriteToFile)
                            {
                                request.InputStream.Flush();
                                request.InputStream.Close();
                                request.InputStream.Dispose();
                                //只读
                                request.InputStream = new System.IO.FileStream(request.TempChaceFileFullPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
                            }
                            else
                            {
                                byte[] buffer = (request.InputStream as System.IO.MemoryStream).ToArray();
                                request.InputStream.Close();
                                //只读
                                request.InputStream = new System.IO.MemoryStream(buffer, false);
                            }
                            requestReadStep = 3;
                            continue;
                        }
                        #endregion 

                        break;
                    case 3:

                        #region -- 一次完整的请求数据读取结束 --

                        if (request.ContentLength > 0)
                        {
                            if (!BodyAnalyzer())
                            {
                                requestReadStep = 4;
                                continue;
                            }
                        }
                        readContentLength = 0;
                        isBreak = true;
                        request.ReceivingTs = DateTime.Now - receivingTime;
                        receivingCount++;
                        #endregion

                        break;
                    default://错误(4或其它)
                        isBreak = true;
                        requestReadStep = 0;
                        if (receiveErrorHttpStatus == Status.Continue)
                        {
                            receiveErrorHttpStatus = Status.Bad_Request;
                        }
                        bytes.Clear();
                        request.ReceivingTs = DateTime.Now - receivingTime;
                        break;
                }
                if (isBreak)
                {
                    break;
                }
            }
            if (requestReadStep == 3)
            {
                requestReadStep = 0;//等待同一连接的下一次请求
                return true;
            }
            return false;
        }

        private bool BodyAnalyzer()
        {
            if (request.ContentLength < 1) { return true; }
            if (request.ContentType.IndexOf("application/x-www-form-urlencoded") > -1)
            {
                var parser = new FormUrlEncodedParser(request.InputStream, Encoding.ASCII);
                bool reval = true;
                parser.Parse((key, offset, length, mayDecode) =>
                {
                    if (string.IsNullOrEmpty(key))
                    {
                        reval = false;
                        ReceiveErrorMsg = "The name cannot be empty.";
                        return false;
                    }
                    if (key.Length > FieldNameLengthLimit)
                    {
                        reval = false;
                        ReceiveErrorMsg = "The name length exceeds the limit.";
                        return false;
                    }
                    if (length > TextFieldLengthLimit)
                    {
                        reval= false;
                        ReceiveErrorMsg = "The length of a single text field exceeds the limit.";
                        return false;//不再继续解析
                    }
                    byte[] buf = new byte[length];
                    request.InputStream.Position = offset;
                    request.InputStream.Read(buf, 0, length);
                    if (mayDecode)
                    {
                        request.Form.Add(key,Utility.UrlDecode(buf, request.Encoding));
                    }
                    else
                    {
                        request.Form.Add(key,request.Encoding.GetString(buf));
                    }
                    return true; // 继续解析
                });
                request.InputStream.Position = 0;
                return reval;
            }
            if (request.ContentType.IndexOf("multipart/form-data") > -1)
            {
                var m = System.Text.RegularExpressions.Regex.Match(request.ContentType, @"boundary=(?:""(?<b>[^""]+)""|(?<b>[^;]+))");
                if (!m.Success)
                {
                    request.InputStream.Close();
                    receiveErrorHttpStatus = Status.Bad_Request;
                    ReceiveErrorMsg = "缺少boundary";
                    return false;
                }
                try
                {
                    var items = MultipartFormDataParser.Parse(request.InputStream, m.Groups["b"].Value);
                    foreach (var item in items)
                    {
                        string key = item.Name;
                        if (string.IsNullOrEmpty(key))
                        {
                            throw new Exception("The name cannot be empty.");
                        }
                        if (key.Length > FieldNameLengthLimit)
                        {
                            throw new Exception("The name length exceeds the limit.");
                        }
                        if (item.HasFileName)
                        {
                            if (!request.Files.ContainsKey(key)) {
                                request.Files.Add(key, new List<UploadFile>());
                            }
                            request.Files[key].Add(new UploadFile(request.InputStream, item.DataOffset, item.DataLength, item.ContentType, item.FileName));
                        }
                        else
                        {
                            if(item.DataLength > TextFieldLengthLimit)
                            {
                                throw new Exception("The length of a single text field exceeds the limit.");
                            }
                            request.Form.Add(key, item.ReadAsString(request.Encoding));
                        }
                    }
                }
                catch (Exception ex)
                {
                    ReceiveErrorMsg = (ex.Message);
                    receiveErrorHttpStatus = Status.Bad_Request;
                    return false;
                }
            }
            return true;
        }
        private bool HeaderAnalysis(string headerLines)
        {
            bool reval = true;
            try
            {
                string[] s1 = headerLines.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in s1)
                {
                    int index = s.IndexOf(':');
                    if (index > 0)
                    {
                        string key = s.Substring(0, index);
                        string val = index + 1 < s.Length ? s.Substring(index + 1).TrimStart() : "";
                        request.Header.Add(key, val);
                    }
                }
                string contentLength = request.Header["Content-Length"];
                if (!string.IsNullOrEmpty(contentLength))
                {
                    request.ContentLength = Convert.ToInt64(contentLength);
                }
                if (!string.IsNullOrEmpty(request.Header["Content-Type"]))
                {
                    request.ContentType = request.Header["Content-Type"].Trim();
                }
                else
                {
                    request.ContentType = "";
                }
                if (!string.IsNullOrEmpty(request.Header["Connection"]))
                {
                    request.Connection = request.Header["Connection"].Trim().ToLower();
                }
                else
                {
                    request.Connection = "close";
                }
                request.Encoding = GetEncoding();
                if (!string.IsNullOrEmpty(request.Header["Cookie"]))
                {
                    string[] ss = request.Header["Cookie"].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string s in ss)
                    {
                        int b = s.IndexOf('=');
                        if (b > 0 && b < s.Length - 1)
                        {
                            string c = s.Substring(0, b).Trim();
                            string d = s.Substring(b + 1).Trim();
                            request.Cookies.Add(c, d);
                        }
                    }
                }
                string hostName = request.Header["Host"];
                if (string.IsNullOrEmpty(hostName))
                {
                    hostName = httpServer.HostName;
                }
                string userAgent = request.Header["User-Agent"];
                if (string.IsNullOrEmpty(userAgent))
                {
                    userAgent = string.Format("None (FROM {0})", userRemoteAddress);
                }
                request.UserAgent = userAgent;
                request.URI = new Uri(string.Format("{0}://{1}{2}", httpServer.UseHttps ? "https" : "http", hostName, request.Path));
            }
            catch { reval = false; }
            return reval;
        }
        private bool RequestLineAnalysis(string requestLine)
        {
            bool reval = true;
            try
            {
                string[] s1 = requestLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (s1.Length < 3)
                {
                    receiveErrorHttpStatus = Status.Bad_Request;
                    return false;
                }
                request = new Request();
                request.UserRemoteAddress = this.userRemoteAddress;
                request.UserRemotePort = this.userRemotePort;
                request.Method = s1[0];
                request.Path = s1[1];
                request.HttpVersion = s1[2];
                request.Header = new System.Collections.Specialized.NameValueCollection();
                int index = request.Path.IndexOf('?');
                if (index > 0 && index + 1 < request.Path.Length)
                {
                    request.QueryString = Utility.ParseQueryString(request.Path.Substring(index + 1));
                }
                else
                {
                    request.QueryString = new System.Collections.Specialized.NameValueCollection();
                }
                request.Form = new System.Collections.Specialized.NameValueCollection();
                request.Files = new Dictionary<string, List<UploadFile>>();
                request.Cookies = new System.Collections.Specialized.NameValueCollection();

            }
            catch { reval = true; }
            return reval;
        }
        private int ByteArrayIndexOf(byte[] source, byte[] frame, int sourceIndex)
        {
            if (sourceIndex > -1 && frame.Length > 0 && frame.Length + sourceIndex <= source.Length)
            {
                int myLen = source.Length - frame.Length + 1;
                int i = sourceIndex;
                while (i < myLen)
                {
                    if (source[i] == frame[0])
                    {
                        if (frame.Length < 2) { return i; }
                        bool flag = true;
                        int j = 1;
                        while (j < frame.Length)
                        {
                            if (source[i + j] != frame[j])
                            {
                                flag = false;
                                break;
                            }
                            j++;
                        }
                        if (flag) { return i; }
                    }
                    i++;
                }
            }
            return -1;
        }
        private System.Text.Encoding GetEncoding()
        {
            System.Text.Encoding encoding = System.Text.Encoding.UTF8;
            var m = System.Text.RegularExpressions.Regex.Match(request.ContentType, @"charset=(?<c>[a-z0-9\-_]{2,20})");
            if (m.Success)
            {
                var charset = m.Groups["c"].Value.ToLower().Replace(" ", "");
                switch (charset)
                {
                    case "ascii":
                        encoding = System.Text.Encoding.ASCII;
                        break;
                    case "gb2312":
                        encoding = System.Text.Encoding.GetEncoding("GB2312");
                        break;
                    case "gbk":
                        encoding = System.Text.Encoding.GetEncoding("GBK");
                        break;
                    case "utf7":
#if NET20
                        encoding = System.Text.Encoding.UTF7;
#else
                        encoding = System.Text.Encoding.UTF8;
#endif
                        break;
                    case "utf16":
                        encoding = System.Text.Encoding.BigEndianUnicode;
                        break;
                    case "bigendianunicode":
                        encoding = System.Text.Encoding.BigEndianUnicode;
                        break;
                    case "utf32":
                        encoding = System.Text.Encoding.UTF32;
                        break;
                    case "unicode":
                        encoding = System.Text.Encoding.Unicode;
                        break;
                }
            }
            return encoding;
        }
    }
}
