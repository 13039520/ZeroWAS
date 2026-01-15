using System;
using System.IO;
using System.Text;

namespace ZeroWAS.Http
{
    public static class FormUrlEncodedParser
    {
        private enum State { Name, Value }

        public static void Parse(Stream stream, FormFieldIndexInfoCallback callback)
        {
            const int BufferSize = 8192;
            byte[] buffer = new byte[BufferSize];
            long globalOffset = 0;
            long streamLength = stream.Length;
            State state = State.Name;

            long nameStart = -1;
            int nameLength = 0;
            long valueStart = -1;
            int valueLength = 0;

            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < bytesRead; i++, globalOffset++)
                {
                    byte b = buffer[i];

                    if (state == State.Name)
                    {
                        if (nameStart < 0)
                        {
                            if (globalOffset >= streamLength)//重要的判断
                            {
                                return;
                            }
                            nameStart = globalOffset;
                        }

                        if (b == (byte)'=')
                        {
                            state = State.Value;
                            valueStart = globalOffset + 1;
                        }
                        else
                        {
                            nameLength++;
                        }
                    }
                    else // state == Value
                    {
                        if (b == (byte)'&')
                        {
                            if (!callback(new FormFieldIndexInfo
                            {
                                NameOffset = nameStart,
                                NameLength = nameLength,
                                ValueOffset = valueStart,
                                ValueLength = valueLength
                            })) { return; }
                            long endIndex = valueStart + valueLength;
                            // reset 状态
                            state = State.Name;
                            nameStart = -1;
                            nameLength = 0;
                            valueStart = -1;
                            valueLength = 0;

                        }
                        else
                        {
                            valueLength++;
                        }
                    }
                }
            }

            // 循环结束后，只 callback 尚未 yield 的字段
            if (nameStart > -1 && nameLength > 0)
            {
                callback(new FormFieldIndexInfo
                {
                    NameOffset = nameStart,
                    NameLength = nameLength,
                    ValueOffset = valueStart >= 0 ? valueStart : nameStart + nameLength,
                    ValueLength = valueLength
                });
            }
        }
    }
    public delegate bool FormFieldIndexInfoCallback(FormFieldIndexInfo info);
    public sealed class FormFieldIndexInfo
    {
        public long NameOffset;   // name 起始 byte 偏移
        public int NameLength;    // name 长度（byte）
        public long ValueOffset;  // value 起始 byte 偏移
        public int ValueLength;   // value 长度（byte）

        public override string ToString() { return $"Name[{NameOffset},{NameLength}] Value[{ValueOffset},{ValueLength}]"; }
    }
}
