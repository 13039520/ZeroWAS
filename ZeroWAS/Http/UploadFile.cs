using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Http
{
    public class UploadFile
    {
        private System.IO.Stream _RawStream = null;
        private long _InRawStreamIndex;
        private long _ContentLength;
        private string _ContentType;
        private string _FileName;

        public string FileName { get { return _FileName; } }
        public string ContentType { get { return _ContentType; } }
        public long ContentLength { get { return _ContentLength; } }



        public UploadFile(System.IO.Stream _RawStream, long _InRawStreamIndex, long _ContentLength, string _ContentType, string _FileName)
        {
            this._RawStream = _RawStream;
            this._InRawStreamIndex = _InRawStreamIndex;
            this._ContentLength = _ContentLength;
            this._ContentType = _ContentType;
            this._FileName = _FileName;
        }


        public void SaveAs(string filePath)
        {
            bool exists = System.IO.File.Exists(filePath);
            using (System.IO.FileStream fs = new System.IO.FileStream(
                filePath,
                exists ? System.IO.FileMode.Truncate : System.IO.FileMode.Create,
                System.IO.FileAccess.Write,
                System.IO.FileShare.Read))
            {
                fs.Position = 0;
                if (_RawStream != null && _RawStream.CanRead && _ContentLength > 0 && _RawStream.Length > _ContentLength)
                {
                    long beginIndex = _InRawStreamIndex;
                    long endIndex = _InRawStreamIndex + _ContentLength;
                    long rLen = 2097152;//2M
                    if (_ContentLength < rLen)
                    {
                        rLen = _ContentLength;
                    }
                    _RawStream.Position = beginIndex;

                    while (beginIndex< endIndex)
                    {
                        byte[] buffer = new byte[rLen];
                        int readLen = _RawStream.Read(buffer, 0, buffer.Length);
                        if (readLen < 1) { break; }
                        if (buffer.Length > readLen)
                        {
                            byte[] t = new byte[buffer.Length];
                            Array.Copy(buffer, t, t.Length);
                            buffer = t;
                        }
                        beginIndex += buffer.Length;
                        fs.Write(buffer, 0, buffer.Length);

                    }                    
                }
                fs.Dispose();
            }
        }
    }
}
