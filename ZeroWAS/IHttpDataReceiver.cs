using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IHttpDataReceiver
    {
        IHttpRequest RequestData { get; }
        Http.Status ReceiveErrorHttpStatus { get; }

        bool Receive(byte[] bytes);
        void CleanUp();


    }
}
