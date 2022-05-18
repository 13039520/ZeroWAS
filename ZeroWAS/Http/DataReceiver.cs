using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

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
        bool isPost = false;
        long clinetId = 0;
        long readContentLength = 0;
        bool isFormDataWriteToFile = false;
        readonly int formDataWriteFileLimit = 4194304;//4M
        readonly byte[] newlineCharBytes = new byte[] { 13, 10 };
        string formDataCacheFilePath = string.Empty;
        DateTime receivingTime = DateTime.Now;
        int receivingCount = 0;

        public IHttpRequest RequestData { get { return request; } }
        public Status ReceiveErrorHttpStatus { get { return receiveErrorHttpStatus; } }

        public DataReceiver(string userRemoteAddress,string userRemotePort, long clinetId, IWebApplication httpServer)
        {
            this.userRemoteAddress = userRemoteAddress;
            this.userRemotePort = userRemotePort;
            this.httpServer = httpServer;
            this.clinetId = clinetId;
        }

        public void CleanUp()
        {
            try
            {
                if (request != null && request.InputStream != null)
                {
                    request.InputStream.Close();
                    if (!string.IsNullOrEmpty(formDataCacheFilePath))
                    {
                        System.IO.File.Delete(formDataCacheFilePath);
                        formDataCacheFilePath = "";
                    }
                }
            }
            catch { }
        }
        public bool Receive(byte[] myBytes)
        {
            if (requestReadStep < 1)
            {
                receivingTime = DateTime.Now;
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
                                continue;
                            }
                            //移除请求头部
                            bytes.RemoveRange(0, index2 + doubleNewlineCharBytes.Length);

                            if (!isPost|| request.ContentLength < 1)
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
                                string dir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_cache_");
                                if (!System.IO.Directory.Exists(dir))
                                {
                                    System.IO.Directory.CreateDirectory(dir);
                                }
                                isFormDataWriteToFile = true;
                                formDataCacheFilePath = System.IO.Path.Combine(dir, clinetId + ".data");
                                request.InputStream = new System.IO.FileStream(formDataCacheFilePath, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read);
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
                                request.InputStream = new System.IO.FileStream(formDataCacheFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
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
                                request.InputStream = new System.IO.FileStream(formDataCacheFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
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
                        
                        if (isPost&& request.ContentLength > 0)
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
                if (!FormInputQueryStreamAnalysis(request.InputStream))
                {
                    receiveErrorHttpStatus = Status.Bad_Request;
                    return false;
                }
                return true;
            }
            if (request.ContentType.IndexOf("multipart/form-data") > -1)
            {
                var m = System.Text.RegularExpressions.Regex.Match(request.ContentType, @"boundary=(?<b>[\-a-zA-Z0-9]{1,100})");
                if (!m.Success)
                {
                    request.InputStream.Close();
                    receiveErrorHttpStatus = Status.Bad_Request;
                    return false;
                }
                if (!FormDataAnalysisByBlock(request.InputStream, m.Groups["b"].Value))
                {
                    receiveErrorHttpStatus = Status.Bad_Request;
                    return false;
                }
            }

            return true;
        }
        private bool FormInputQueryStreamAnalysis(System.IO.Stream stream)
        {
            bool reval = true;
            try
            {
                if (stream == null || !stream.CanRead)
                {
                    return false;
                }
                //38='&'
                byte[] symbolBytes1 = new byte[] { 38 };
                //61='='
                byte[] symbolBytes2 = new byte[] { 61 };
                byte[] tempBytes = new byte[2097152];//2M
                List<long> boundaryIndex = new List<long>();
                long index = 0;
                long streamLength = stream.Length;
                while (index < streamLength)
                {
                    long rLen = tempBytes.Length;
                    long endIndex = index + rLen;
                    if (endIndex >= streamLength)
                    {
                        endIndex = streamLength - 1;
                        rLen = endIndex - index;
                        if (rLen < 1)
                        {
                            break;
                        }
                        tempBytes = new byte[rLen];
                    }
                    stream.Position = index;
                    stream.Read(tempBytes, 0, tempBytes.Length);

                    int frameCount = 0;
                    int tempIndex = 0;
                    int frameIndex = ByteArrayIndexOf(tempBytes, symbolBytes1, tempIndex);
                    while (frameIndex != -1)
                    {
                        boundaryIndex.Add(index + frameIndex);
                        frameCount++;
                        tempIndex = (frameIndex + symbolBytes1.Length);
                        frameIndex = ByteArrayIndexOf(tempBytes, symbolBytes1, tempIndex);
                    }
                    if (rLen > symbolBytes1.Length)
                    {
                        index += (rLen - symbolBytes1.Length);
                    }
                    else
                    {
                        index += rLen;
                    }
                }
                //位置回退
                stream.Position = 0;
                //没有分隔符：当成一个name/value对进行解析并退出
                if (boundaryIndex.Count < 1)
                {
                    if (streamLength > 5242880)
                    {
                        return true;//返回：内容太长无需解析
                    }
                    byte[] temp = new byte[streamLength];
                    stream.Read(temp, 0, temp.Length);
                    FormInputQuerySplitNameValue(temp, symbolBytes2);
                    return true;
                }
                //有分隔符则循环处理各个片段
                if (boundaryIndex[0] != 0)
                {
                    boundaryIndex.Insert(0, 0);
                }
                if (boundaryIndex[boundaryIndex.Count-1] != streamLength-1)
                {
                    boundaryIndex.Add(streamLength);
                }
                int limit = boundaryIndex.Count - 1;
                for (int i = 0; i < limit; i++)
                {
                    long frameLen = boundaryIndex[i + 1] - boundaryIndex[i];
                    long startIndex = boundaryIndex[i];
                    if (i > 0)
                    {
                        //忽略前置的'&'字符
                        startIndex += 1;
                        if (i < limit - 1)
                        {
                            frameLen -= 1;
                        }
                    }
                    if (frameLen < 2)//至少两个字符，比如：a=
                    {
                        continue;
                    }
                    /*if (frameLen > 5242880)
                    {
                        throw new Exception("单个文本字段的长度超出5M的限制");
                    }*/
                    stream.Position = startIndex;
                    byte[] temp = new byte[frameLen];
                    stream.Read(temp, 0, temp.Length);

                    FormInputQuerySplitNameValue(temp, symbolBytes2);
                }
            }
            catch (Exception ex)
            {
                reval = false;
                Console.WriteLine("【FormData】错误={0}", ex.Message);
            }
            return reval;
        }
        private bool FormInputQuerySplitNameValue(byte[] frame,byte[] symbolBytes)
        {
            int n = (int)ByteArrayIndexOf(frame, symbolBytes, 0);
            if (n < 1)//缺少key
            {
                return false;
            }
            string name = request.Encoding.GetString(frame, 0, n).Trim();
            if (name.Length < 1)
            {
                return false;
            }
            string value = "";
            int skipCount = n + 1;
            if (skipCount < frame.Length)
            {
                byte[] val = new byte[frame.Length - skipCount];
                Array.Copy(frame, skipCount, val, 0, val.Length);
                value = Utility.UrlDecode(val, request.Encoding);
            }
            request.Form.Add(name, value);
            return true;
        }
        private bool FormDataAnalysisByBlock(System.IO.Stream stream, string boundary)
        {
            //DateTime receivingTime = DateTime.Now;
            bool reval = true;
            try
            {
                List<long> boundaryIndex = new List<long>();
                boundary = "--" + boundary;
                byte[] boundaryBytes = request.Encoding.GetBytes(boundary);
                int boundaryLength = boundaryBytes.Length;

                #region -- 预先提取各个数据片段在流中的索引位置 --

                byte[] tempBytes = new byte[2097152];//2M
                long streamLength = stream.Length;
                long index = 0;
                while (index < streamLength)
                {
                    long rLen = tempBytes.Length;
                    long endIndex = index + rLen;
                    if (endIndex > streamLength)
                    {
                        endIndex = streamLength;
                        rLen = endIndex - index;
                        if (rLen < 1)
                        {
                            break;
                        }
                        tempBytes = new byte[rLen];
                    }
                    stream.Position = index;
                    stream.Read(tempBytes, 0, tempBytes.Length);

                    int boundaryCount = 0;
                    int tempIndex = 0;
                    int frameIndex = ByteArrayIndexOf(tempBytes, boundaryBytes, tempIndex);
                    while (frameIndex != -1)
                    {
                        boundaryIndex.Add(index + frameIndex);
                        tempIndex = frameIndex + boundaryLength;
                        boundaryCount++;
                        frameIndex = ByteArrayIndexOf(tempBytes, boundaryBytes, tempIndex);
                    }
                    //index += tempIndex;
                    //在片段中有分隔符
                    if (boundaryCount > 0)
                    {
                        long dif = rLen - tempIndex;
                        if(dif> boundaryLength)//剩余字符超出分隔符长度
                        {
                            index += (tempIndex + dif - boundaryLength);
                        }
                        else//剩余字符不足或刚好是分隔符长度
                        {
                            index += (tempIndex + boundaryLength);
                        }
                    }
                    else//在片段中缺少分隔符
                    {
                        if (rLen > boundaryLength)
                        {
                            index += (rLen - boundaryLength);
                        }
                        else
                        {
                            index += rLen;
                        }
                    }
                }
                if (boundaryIndex.Count < 1)
                {
                    throw new Exception("缺少分隔符");
                }
                stream.Position = boundaryIndex[boundaryIndex.Count - 1] + boundaryLength;
                byte[] bytes = new byte[2];
                stream.Read(bytes, 0, bytes.Length);
                if (bytes[0] != 45 && bytes[1] != 45)
                {
                    throw new Exception("缺少结束符");
                }

                #endregion

                #region -- 分析片段(最后一个索引编号其实是结束符的开始位置) --
                byte[] newlineChars = new byte[] { 13, 10 };
                for(int i=0;i< boundaryIndex.Count-1; i++)
                {
                    long frameLen = boundaryIndex[i + 1] - boundaryIndex[i];
                    long startIndex = boundaryIndex[i];
                    int skipCount = boundaryLength + 2;//跳过分隔符+回车换行
                    startIndex += skipCount;
                    //开始读取字段描述
                    long tryLen = 10240;//10k
                    if (skipCount + tryLen > frameLen)
                    {
                        tryLen = frameLen - skipCount;
                        if (tryLen < 1)
                        {
                            throw new Exception("片段组合错误");
                        }
                    }
                    stream.Position = startIndex;
                    byte[] temp = new byte[tryLen];
                    stream.Read(temp, 0, temp.Length);
                    int newlineCharIndex = (int)ByteArrayIndexOf(temp, newlineChars, 0);
                    if (newlineCharIndex == -1)
                    {
                        throw new Exception("描述行过长");
                    }
                    string disposition = request.Encoding.GetString(temp, 0, newlineCharIndex);
                    if (disposition.IndexOf("Content-Disposition:") < 0)
                    {
                        throw new Exception("描述行没有出现在应有的位置");
                    }
                    var d = System.Text.RegularExpressions.Regex.Match(disposition, @"name=""(?<name>.+?)""");
                    if (!d.Success)
                    {
                        throw new Exception("描述行错误");
                    }
                    string fieldName = d.Groups["name"].Value;
                    string fieldFileName = "";
                    bool isFileField = false;
                    d = System.Text.RegularExpressions.Regex.Match(disposition, @"filename=""(?<name>.+?)""");
                    if (d.Success)
                    {
                        fieldFileName = Utility.UrlDecode(d.Groups["name"].Value);
                        isFileField = true;
                    }
                    //读取下一行
                    skipCount += (newlineCharIndex + 2);
                    startIndex += (newlineCharIndex + 2);

                    if (isFileField)//文件字段
                    {
                        tryLen = 10240;//10k
                        if (skipCount + tryLen > frameLen)
                        {
                            tryLen = frameLen - skipCount;
                            if (tryLen < 1)
                            {
                                throw new Exception("片段组合错误");
                            }
                        }
                        temp = new byte[tryLen];
                        stream.Position = startIndex;
                        stream.Read(temp, 0, temp.Length);
                        newlineCharIndex = (int)ByteArrayIndexOf(temp, newlineChars, 0);
                        if (newlineCharIndex == -1)
                        {
                            throw new Exception("文件内容类型描述行过长");
                        }
                        string contentType = request.Encoding.GetString(temp, 0, newlineCharIndex);
                        if (contentType.IndexOf("Content-Type:") < 0)
                        {
                            throw new Exception("文件内容类型描述行错误");
                        }
                        int n = contentType.IndexOf(':');
                        contentType = n + 1 >= contentType.Length ? "" : contentType.Substring(n + 1);
                        contentType = contentType.Trim();
                        if (string.IsNullOrEmpty(contentType))
                        {
                            throw new Exception("文件内容类型描述行错误");
                        }
                        skipCount += (newlineCharIndex + 4);
                        startIndex += (newlineCharIndex + 4);
                        tryLen = frameLen - skipCount - 2;//排除作为字段结束符的回车换行
                        if (tryLen < 0)
                        {
                            throw new Exception("片段组合错误");
                        }
                        request.Form.Add(fieldName, fieldFileName);
                        if (!request.Files.ContainsKey(fieldName))
                        {
                            List<UploadFile> httpUploadFiles = new List<UploadFile>();
                            httpUploadFiles.Add(new UploadFile(stream, startIndex, tryLen, contentType, fieldFileName));
                            request.Files.Add(fieldName, httpUploadFiles);
                        }
                        else
                        {
                            request.Files[fieldName].Add(new UploadFile(stream, startIndex, tryLen, contentType, fieldFileName));
                        }
                    }
                    else//文本字段
                    {
                        //跳过空行
                        skipCount += 2;
                        startIndex += 2;
                        tryLen = frameLen - skipCount - 2;//排除作为字段结束符的回车换行
                        if (tryLen < 0)
                        {
                            throw new Exception("片段组合错误");
                        }
                        /*
                        if(tryLen > 5242880)
                        {
                            throw new Exception("单个文本字段的长度超出5M的限制");
                        }*/
                        temp = new byte[tryLen];
                        stream.Position = startIndex;
                        stream.Read(temp, 0, temp.Length);
                        string text = request.Encoding.GetString(temp);
                        request.Form.Add(fieldName, text);
                    }
                }


                #endregion

                //Console.WriteLine("【FormData】流长度={0}&分析耗时={2}s&方式=块读取", stream.Length, (DateTime.Now - receivingTime).TotalSeconds);
            }
            catch(Exception ex)
            {
                reval = false;
                Console.WriteLine("【FormData】错误={0}", ex.Message);
            }
            
            return reval;
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
                if (isPost)
                {
                    if (string.IsNullOrEmpty(request.Header["Content-Length"]))
                    {
                        receiveErrorHttpStatus = Status.Length_Required;
                        return false;
                    }
                    request.ContentLength = Convert.ToInt64(request.Header["Content-Length"]);
                }
                if (!string.IsNullOrEmpty(request.Header["Content-Type"]))
                {
                    request.ContentType = request.Header["Content-Type"].Trim();
                }
                else
                {
                    request.ContentType = "application/x-www-form-urlencoded";
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
                string hostName = request.Header["HostName"];
                if (string.IsNullOrEmpty(hostName))
                {
                    hostName = httpServer.HostName;
                }
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


                isPost = s1[0] == "POST";
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
