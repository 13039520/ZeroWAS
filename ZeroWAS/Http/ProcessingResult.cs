using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Http
{
    public class ProcessingResult: IHttpProcessingResult
    {
        public TimeSpan ReceivingTs { get; set; }
        public TimeSpan ProcessingTs { get; set; }
        public long ReceivedContentLength { get; set; }
        public long ReturnedContentLength { get; set; }
        public string RequestHttpLine { get; set; }
        public string RequestHeader { get; set; }
        public string ResponseHttpLine { get; set; }
        public string ResponseHeader { get; set; }

    }
}
