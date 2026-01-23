using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZeroWAS.Common;

namespace ZeroWAS.RawSocket
{
    internal class MemoryMappedBuffer : IPayloadBuffer
    {
        private readonly string _tempFile;
#if !NET20
        private readonly System.IO.MemoryMappedFiles.MemoryMappedFile _mmf;
#else
            private readonly FileStream _fs;
            private readonly object _lock = new object();  
#endif
        private const int ChunkSize = 2048;

        public MemoryMappedBuffer(byte[] header, Stream content)
        {
            _tempFile = GetTempFile();
            using (var fs = File.OpenWrite(_tempFile))
            {
                fs.Write(header, 0, header.Length);
                if (content != null) { CopyStream.Copy(content, fs); }
                fs.Flush();
            }
#if !NET20
            _mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(_tempFile, FileMode.Open);
#else
            _fs = new FileStream(_tempFile, FileMode.Open, FileAccess.Read, FileShare.Read);
#endif
        }

        public MemoryMappedBuffer(Stream serializedStream)
        {
            _tempFile = GetTempFile();
            using (var fs = File.OpenWrite(_tempFile))
            {
                CopyStream.Copy(serializedStream, fs);
                fs.Flush();
            }
#if !NET20
            _mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(_tempFile, FileMode.Open);
#else
            _fs = new FileStream(_tempFile, FileMode.Open, FileAccess.Read, FileShare.Read);
#endif
        }

        public void ForEachSegment(Action<byte[]> callback)
        {
#if !NET20
            using (var view = _mmf.CreateViewStream())
            {
                byte[] buffer = new byte[ChunkSize];
                int read;
                while ((read = view.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (read < buffer.Length)
                    {
                        byte[] temp = new byte[read];
                        Array.Copy(buffer, temp, read);
                        callback(temp);
                    }
                    else
                    {
                        callback((byte[])buffer.Clone());
                    }
                }
            }
#else
                lock (_lock) // 保证线程安全
                {
                    _fs.Position = 0;
                    byte[] buffer = new byte[ChunkSize];
                    int read;
                    while ((read = _fs.Read(buffer, 0, buffer.Length)) > 0) {
                        if (read < buffer.Length)
                        {
                            byte[] temp = new byte[read];
                            Array.Copy(buffer, temp, read);
                            callback(temp);
                        }
                        else
                        {
                            callback((byte[])buffer.Clone());
                        }
                    }
                }
#endif
        }

        public void Dispose()
        {
#if !NET20
            _mmf.Dispose();
#else
            _fs.Dispose();
#endif
            if (File.Exists(_tempFile)) { 
                File.Delete(_tempFile);
#if DEBUG
                Console.WriteLine("[{0}]Delete=>{1}", this.GetType().Name, _tempFile);
#endif
            }
        }
        private string GetTempFile()
        {
            return TempFile.GetTempFileName("rs-");
        }
    }
}
