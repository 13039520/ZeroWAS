using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    internal class ListBuffer : IPayloadBuffer
    {
        private readonly List<byte[]> _segments;
        public ListBuffer(List<byte[]> segments) { _segments = segments; }
        public void ForEachSegment(Action<byte[]> callback)
        {
            foreach (var seg in _segments) callback(seg);
        }
        public void Dispose() { }
    }
}
