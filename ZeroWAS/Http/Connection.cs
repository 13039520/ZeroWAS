using System;
using System.Collections.Generic;
using System.Text;

#region -- 扩展(Net 2.0)：增加安全层系统枚举 --
#if NET20
namespace System.Net
{
    using System.Security.Authentication;
    public static class SecurityProtocolTypeExtensions
    {
        public const SecurityProtocolType Tls12 = (SecurityProtocolType)SslProtocolsExtensions.Tls12;
        public const SecurityProtocolType Tls11 = (SecurityProtocolType)SslProtocolsExtensions.Tls11;
        public const SecurityProtocolType SystemDefault = (SecurityProtocolType)0;
    }
}
namespace System.Security.Authentication
{
    public static class SslProtocolsExtensions
    {
        public const SslProtocols Tls12 = (SslProtocols)0x00000C00;
        public const SslProtocols Tls11 = (SslProtocols)0x00000300;
    }
}
#endif
#endregion

namespace ZeroWAS.Http
{
    public class Connection<TUser> : IHttpConnection<TUser>
    {
        bool _Dispose = false;
        bool _Disposed = false;
        private System.Security.Cryptography.X509Certificates.X509Certificate x509Cer = null;
        private System.Net.Sockets.Socket socketAccepter = null;
        private System.Net.Security.SslStream sslStream = null;
        private System.Net.Sockets.NetworkStream ns = null;
        private IWebApplication webApp = null;
        IHttpDataReceiver receiver = null;
        bool _IsHttps = false;
        private DateTime _LastActivityTime = DateTime.Now;

        public string WebSocketChannelPath { get; set; }
        public string RawSocketChannelPath { get; set; }
        public TUser User { get; set; }
        public long ClinetId { get; set; }
        public int HttpRequestCount { get; set; }
        public bool IsDataMasked { get; set; }
        public string RemoteEndPointStr { get; set; }
        public bool LastHttpInProcess { get; set; }
        private Common.SocketTypeEnum _SocketType = Common.SocketTypeEnum.Http;
        public Common.SocketTypeEnum SocketType { get { return _SocketType; } set { _SocketType = value; } }

        public DateTime LastActivityTime { get { return _LastActivityTime; } set { _LastActivityTime = value; } }

        public bool IsHttps { get { return _IsHttps; } }
        public Http.Handlers.ErrorHandler OnErrorHandler { get; set; }
        public Http.Handlers.RawStreamReceivedHandler<TUser> OnRawStreamReceivedHandler { get; set; }
        public event HttpSocketDisposedHandler<TUser> OnDisposed;

        public Connection(System.Net.Sockets.Socket socketAccepter, IWebApplication webApp, System.Security.Cryptography.X509Certificates.X509Certificate x509Cer)
        {
            if (socketAccepter == null)
            {
                throw new Exception("socketAccepter can not be null");
            }
            if (x509Cer != null)
            {
                _IsHttps = true;
            }
            this.socketAccepter = socketAccepter;
            this.webApp = webApp;
            this.x509Cer = x509Cer;
        }



        public void Working()
        {
            try
            {
                if (this == null || this._Dispose)
                {
                    throw new ObjectDisposedException("HttpSocket");
                }
                Common.SocketManager<TUser>.AddHttpSocket(this);

                //Console.WriteLine("[Connected] Number of current connections:{0},clientId={1}", HttpSocketManager<TUser>.GetCount(), this.ClinetId);

                string remoteEndPointStr = socketAccepter.RemoteEndPoint.ToString();
                string userRemoteAddress = System.Text.RegularExpressions.Regex.Replace(remoteEndPointStr, @":\d{1,5}$", "");
                string userRemotePort = System.Text.RegularExpressions.Regex.Replace(remoteEndPointStr, @"^.[^:]+:", "");

                int len = 2048;
                if (IsHttps)
                {
                    this.ns = new System.Net.Sockets.NetworkStream(socketAccepter, false);
                    this.ns.WriteTimeout = 15000;
                    this.ns.ReadTimeout = 15000;
                    this.sslStream = new System.Net.Security.SslStream(this.ns);
                    this.sslStream.ReadTimeout = 15000;
                    this.sslStream.WriteTimeout = 15000;

                    try
                    {
#if NET20
                        //开始握手协商
                        this.sslStream.AuthenticateAsServer(x509Cer, false, System.Security.Authentication.SslProtocolsExtensions.Tls12, true);
#else
                        this.sslStream.AuthenticateAsServer(x509Cer, false, System.Security.Authentication.SslProtocols.Tls12, true);
#endif
                    }
                    catch(Exception ex)
                    {
                        this.Dispose(ex);
                        return;
                    }
                    receiver = new DataReceiver<TUser>(userRemoteAddress, userRemotePort, this.ClinetId, this.webApp);
                    byte[] buffer = new byte[len];
                    int bound = -1;
                    while (bound != 0)
                    {
                        if (_Dispose) { break; }
                        try
                        {
                            //读取时会报异常：确定基础流没有关闭时可以忽略
                            bound = sslStream.Read(buffer, 0, buffer.Length);
                            if (bound>0&&bound < buffer.Length)
                            {
                                byte[] temp = new byte[bound];
                                Array.Copy(buffer, 0, temp, 0, temp.Length);
                                buffer = temp;
                            }
                        }
                        catch {
                            //break;

                            bound = -1;
                            continue;
                        }
                        if (bound < 1)
                        {
                            //Console.WriteLine("[https]接收到0字节，将断开连接……");
                            this.Dispose(new Exception("0 bytes"));
                            break;
                        }
                        FireRawStreamReceivedHandler(buffer);
                        buffer = new byte[len];
                    }
                }
                else
                {
                    receiver = new DataReceiver<TUser>(userRemoteAddress, userRemotePort, this.ClinetId, this.webApp);
                    bool isFirst = true;
                    int bound = -1;
                    while (bound != 0)
                    {
                        if (_Dispose) { break; }
                        byte[] buffer = new byte[len];
                        bound = socketAccepter.Receive(buffer);
                        if (bound < 1)
                        {
                            //Console.WriteLine("[http]接收到0字节，将断开连接……");
                            this.Dispose(new Exception("0 bytes"));
                            break;
                        }
                        if (isFirst)
                        {
                            isFirst = false;
                            byte val = buffer[0];
                            if (val == 22)
                            {
                                //Console.WriteLine(Encoding.UTF8.GetString(buffer));
                                //断开：不接收https请求(握手数据包会导致接收解析进入死循环，因为会一直检测不到换行符)
                                this.Dispose(new Exception("Not http request"));
                                break;
                            }
                        }
                        byte[] real;
                        if (bound < len)
                        {
                            real = new byte[bound];
                            Array.Copy(buffer, real, bound);
                        }
                        else
                        {
                            real = buffer;
                        }
                        FireRawStreamReceivedHandler(real);
                    }
                }

            }catch (Exception ex)
            {
                //FireErrorHandler(ex);
                Dispose(ex);
            }
        }
        public bool Write(byte[] bytes)
        {
            if (_Dispose) { return false; }
            bool reval = true;
            try
            {
                if (IsHttps)
                {
                    sslStream.Write(bytes);
                    sslStream.Flush();
                }
                else
                {
                    socketAccepter.Send(bytes);
                }
            }
            catch (Exception ex)
            {
                Dispose(ex);
                reval = false;
            }
            return reval;
        }
        System.Exception DisposedException = null;
        public void Dispose()
        {
            Dispose(new Exception("Normal"));
        }
        public void Dispose(Exception ex)
        {
            DisposedException = ex;
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_Dispose)
            {
                return;
            }
            _Dispose = true;
            if (disposing)
            {
                try
                {
                    //避免异常影响其它对象的释放
                    Common.SocketManager<TUser>.Remove(this);
                }
                catch { }
                try
                {
                    if (this.sslStream != null)
                    {
                        this.sslStream.Dispose();
                    }
                    if (this.ns != null)
                    {
                        this.ns.Dispose();
                    }
                    if (this.socketAccepter != null)
                    {
                        this.socketAccepter.Close();
                    }
                    if (receiver != null)
                    {
                        receiver.CleanUp();
                    }
                }
                catch { }
                
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            _Dispose = true;
            if (_Disposed) { return; }
            _Disposed = true;
            if (OnDisposed != null)
            {
                try
                {
                    OnDisposed(DisposedException);
                }
                catch { }
            }
        }

        private void FireRawStreamReceivedHandler(byte[] bytes)
        {
            if (OnRawStreamReceivedHandler != null)
            {
                try
                {
                    OnRawStreamReceivedHandler(this, receiver, bytes);
                }
                catch { }
            }
        }
        private void FireErrorHandler(Exception ex)
        {
            if (OnErrorHandler != null)
            {
                try
                {
                    OnErrorHandler(this, ex);
                }
                catch { }
            }
        }
        bool ValidateServerCertificate(object sender,
            System.Security.Cryptography.X509Certificates.X509Certificate certificate,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }
            return true;
        }

        long ByteArrayIndexOf(byte[] source, byte[] frame, long sourceIndex)
        {
            if (source != null &&
                frame != null &&
                source.Length > 0 &&
                frame.Length > 0 &&
                sourceIndex > -1 &&
                source.Length >= frame.Length &&
                frame.Length + sourceIndex < source.Length)
            {
                for (long i = sourceIndex; i < source.Length - frame.Length + 1; i++)
                {
                    if (source[i] == frame[0])
                    {
                        if (frame.Length < 2) { return i; }
                        bool flag = true;
                        for (long j = 1; j < frame.Length; j++)
                        {
                            if (source[i + j] != frame[j])
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (flag) { return i; }
                    }
                }
            }
            return -1;
        }

        public override bool Equals(object obj)
        {
            return (obj as IHttpConnection<TUser>).ClinetId == this.ClinetId;
        }
        public override int GetHashCode()
        {
            return this.ClinetId.GetHashCode() ^ this.ClinetId.GetHashCode();
        }

    }
}


