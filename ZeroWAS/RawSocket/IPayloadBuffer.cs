using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    public interface IPayloadBuffer: IDisposable
    {
        void ForEachSegment(Action<byte[]> callback);
    }
}
