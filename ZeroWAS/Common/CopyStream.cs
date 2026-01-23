using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZeroWAS.Common
{
    internal static class CopyStream
    {
        public static void Copy(Stream source, Stream destination, int bufferSize = 8192)
        {
            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, read);
            }
        }
    }
}
