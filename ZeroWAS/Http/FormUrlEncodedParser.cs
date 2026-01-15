using System;
using System.IO;
using System.Text;

namespace ZeroWAS.Http
{
    public delegate bool FormFieldCallback(string key, long valueOffset, int valueLength, bool mayNeedUrlDecode);
    public sealed class FormUrlEncodedParser
    {
        private readonly Stream _stream;
        private readonly Encoding _encoding;
        private readonly byte[] _buffer;

        public FormUrlEncodedParser(Stream stream, Encoding encoding = null, int bufferSize = 64 * 1024)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            _stream = stream;
            _encoding = encoding ?? Encoding.UTF8;
            _buffer = new byte[bufferSize];
        }

        public void Parse(FormFieldCallback callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            long globalPos = 0;

            MemoryStream keyBuffer = new MemoryStream();

            bool inValue = false;
            bool mayNeedDecode = false;

            long valueStart = -1;
            long fieldStart = 0;

            int read;
            while ((read = _stream.Read(_buffer, 0, _buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    byte b = _buffer[i];

                    if (!inValue)
                    {
                        if (b == (byte)'=')
                        {
                            inValue = true;
                            mayNeedDecode = false;
                            valueStart = globalPos + i + 1;
                        }
                        else if (b == (byte)'&')
                        {
                            // key-only 字段
                            if (!Emit(callback, keyBuffer, -1, 0, false))
                                return;

                            keyBuffer.SetLength(0);
                            fieldStart = globalPos + i + 1;
                        }
                        else
                        {
                            keyBuffer.WriteByte(b);
                        }
                    }
                    else
                    {
                        if (b == (byte)'%' || b == (byte)'+')
                            mayNeedDecode = true;

                        if (b == (byte)'&')
                        {
                            long valueEnd = globalPos + i;
                            int valueLength = (int)(valueEnd - valueStart);

                            if (!Emit(callback, keyBuffer, valueStart, valueLength, mayNeedDecode))
                                return;

                            // reset
                            keyBuffer.SetLength(0);
                            inValue = false;
                            valueStart = -1;
                            mayNeedDecode = false;
                            fieldStart = globalPos + i + 1;
                        }
                    }
                }

                globalPos += read;
            }

            // 最后一个字段
            if (keyBuffer.Length > 0)
            {
                if (inValue)
                {
                    int valueLength = (int)(globalPos - valueStart);
                    Emit(callback, keyBuffer, valueStart, valueLength, mayNeedDecode);
                }
                else
                {
                    Emit(callback, keyBuffer, -1, 0, false);
                }
            }
        }

        private bool Emit(
            FormFieldCallback callback,
            MemoryStream keyBuffer,
            long valueOffset,
            int valueLength,
            bool mayNeedDecode)
        {
            string key = _encoding.GetString(
                keyBuffer.GetBuffer(), 0, (int)keyBuffer.Length);

            return callback(key, valueOffset, valueLength, mayNeedDecode);
        }
    }
}
