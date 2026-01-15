using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZeroWAS.Http
{
    public static class MultipartFormDataParser
    {
        public static List<MultipartItem> Parse(
            Stream bodyStream,
            string boundary,
            Encoding headerEncoding = null)
        {
            if (bodyStream == null)
                throw new ArgumentNullException(nameof(bodyStream));

            if (!bodyStream.CanSeek)
                throw new InvalidOperationException("Multipart parser requires a seekable stream");

            if (string.IsNullOrEmpty(boundary))
                throw new ArgumentNullException(nameof(boundary));

            if (headerEncoding == null)
                headerEncoding = Encoding.UTF8;

            var items = new List<MultipartItem>();

            byte[] boundaryBytes = Encoding.ASCII.GetBytes("--" + boundary);
            byte[] boundaryEndBytes = Encoding.ASCII.GetBytes("--" + boundary + "--");

            long pos = 0;
            long length = bodyStream.Length;

            while (true)
            {
                long boundaryPos = IndexOf(bodyStream, boundaryBytes, pos);
                if (boundaryPos < 0)
                    break;

                long afterBoundary = boundaryPos + boundaryBytes.Length;

                // 判断结束 boundary
                if (Match(bodyStream, afterBoundary, new byte[] { 45, 45 })) // --
                    break;

                // 跳过 CRLF
                if (Match(bodyStream, afterBoundary, new byte[] { 13, 10 }))
                    afterBoundary += 2;

                // headers 结束位置
                long headerEnd = IndexOf(
                    bodyStream,
                    new byte[] { 13, 10, 13, 10 },
                    afterBoundary);

                if (headerEnd < 0)
                    break;

                var item = new MultipartItem();
                item.SourceStream = bodyStream;

                ParseHeaders(
                    bodyStream,
                    afterBoundary,
                    headerEnd - afterBoundary,
                    headerEncoding,
                    item);

                long dataStart = headerEnd + 4;

                long nextBoundary = IndexOf(bodyStream, boundaryBytes, dataStart);
                if (nextBoundary < 0)
                    break;

                long dataEnd = nextBoundary - 2; // 去掉结尾 \r\n
                if (dataEnd < dataStart)
                    dataEnd = dataStart;

                item.DataOffset = dataStart;
                item.DataLength = dataEnd - dataStart;

                items.Add(item);

                pos = nextBoundary;
            }

            return items;
        }

        // ---------------- header parsing ----------------

        private static void ParseHeaders(
            Stream stream,
            long offset,
            long length,
            Encoding encoding,
            MultipartItem item)
        {
            stream.Position = offset;

            byte[] buf = new byte[length];
            stream.Read(buf, 0, buf.Length);

            string headerText = encoding.GetString(buf);
            string[] lines = headerText.Split(
                new[] { "\r\n" },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                int idx = line.IndexOf(':');
                if (idx < 0)
                    continue;

                string key = line.Substring(0, idx).Trim();
                string val = line.Substring(idx + 1).Trim();

                item.Headers[key] = val;

                if (key.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase))
                    ParseContentDisposition(val, item);
                else if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    item.ContentType = val;
            }
        }

        private static void ParseContentDisposition(string value, MultipartItem item)
        {
            string[] parts = value.Split(';');

            foreach (string p in parts)
            {
                string part = p.Trim();

                if (part.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
                {
                    item.Name = TrimQuote(part.Substring(5));
                }
                else if (part.StartsWith("filename=", StringComparison.OrdinalIgnoreCase))
                {
                    item.HasFileName = true;
                    item.FileName = TrimQuote(part.Substring(9));
                }
            }
        }

        private static string TrimQuote(string s)
        {
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
            {
                return s.Substring(1, s.Length - 2);
            }
            return s;
        }

        // ---------------- stream helpers ----------------

        private static long IndexOf(Stream stream, byte[] pattern, long start)
        {
            stream.Position = start;

            int matched = 0;
            long pos = start;

            int b;
            while ((b = stream.ReadByte()) != -1)
            {
                if (b == pattern[matched])
                {
                    matched++;
                    if (matched == pattern.Length)
                        return pos - pattern.Length + 1;
                }
                else
                {
                    matched = 0;
                }
                pos++;
            }

            return -1;
        }

        private static bool Match(Stream stream, long pos, byte[] pattern)
        {
            long old = stream.Position;
            stream.Position = pos;

            for (int i = 0; i < pattern.Length; i++)
            {
                if (stream.ReadByte() != pattern[i])
                {
                    stream.Position = old;
                    return false;
                }
            }

            stream.Position = old;
            return true;
        }
    }

}
