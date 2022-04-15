using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IHttpProcessingResult
    {
        TimeSpan ReceivingTs { get; }
        TimeSpan ProcessingTs { get; }
        long ReceivedContentLength { get; }
        long ReturnedContentLength { get; }
        string RequestHttpLine { get; }
        string RequestHeader { get; }
        string ResponseHttpLine { get; }
        string ResponseHeader { get; }

    }
}
