using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    internal class MemoryBuffer : IPayloadBuffer
    {
        private readonly byte[] _data;
        public MemoryBuffer(byte[] data) { _data = data; }
        public void ForEachSegment(Action<byte[]> callback)
        {
            if (_data.Length > 0) callback(_data);
        }
        public void Dispose() { }
    }
}
