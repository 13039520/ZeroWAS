using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ZeroWAS.RawSocket
{
    public class DataFrameReadArgs : EventArgs
    {
        public readonly byte[] Data;
        public readonly bool IsEnd;
        public readonly IRawSocketData Frame;
        public DataFrameReadArgs(IRawSocketData frame, byte[] data, bool isEnd)
        {
            this.Frame = frame;
            this.Data = data;
            this.IsEnd = isEnd;
        }
    }
    public delegate void DataFrameReadHandler(DataFrameReadArgs e);
    public delegate void DataFrameDisposedHandler(Guid guid);
    public delegate void DataFrameReceivedHandler(IRawSocketData frame);
    public interface IDataFrameReceiver
    {
        event DataFrameReceivedHandler OnReceived;
        void Receive(byte[] input);
    }
    public class DataFrameInfo
    {
        public ushort RemarkLength { get; }
        public long ContentLength { get; }
        public long FrameLength { get; }
        public byte FrameType { get; }
        public DataFrameInfo(DataFrame d)
        {

        }
    }
    public class DataFrame : IRawSocketData
    {
        private Guid _Handler = Guid.NewGuid();
        public Guid Handler { get { return _Handler; } }
        public event DataFrameDisposedHandler OnDisposed = null;
        public byte FrameType { get; set; }
        private string _FrameRemark = string.Empty;
        private byte[] _FrameRemarkBytes = new byte[0];
        public string FrameRemark { get { return _FrameRemark; } 
            set
            {
                _FrameRemark = value;
                if (string.IsNullOrEmpty(_FrameRemark))
                {
                    _FrameRemarkBytes = new byte[0];
                }
                else
                {
                    _FrameRemarkBytes = System.Text.Encoding.UTF8.GetBytes(FrameRemark);
                }
            } 
        }
        System.IO.Stream _FrameContent = null;
        public System.IO.Stream FrameContent
        {
            get { return _FrameContent; }
            set
            {
                if (_FrameContent != null) { _FrameContent.Close(); }
                _FrameContent = value;
            }
        }

        public DataFrame() { }
        public DataFrame(string content, byte type = 1) {
            if (!string.IsNullOrEmpty(content))
            {
                this.FrameContent = new MemoryStream(Encoding.UTF8.GetBytes(content));
            }
            this.FrameType = type;
        }

        private void Callback(DataFrameReadHandler handler, byte[] bytes, bool isEnd)
        {
            handler(new DataFrameReadArgs(this, bytes, isEnd));
        }
        private byte[] GetHead(long contentLen)
        {
            if (_FrameRemarkBytes.Length > ushort.MaxValue)
            {
                throw new Exception("Maximum remark length is " + ushort.MaxValue);
            }
            List<byte> heads = new List<byte>(_FrameRemarkBytes.Length + 11);
            heads.Add(this.FrameType);//1 bytes
            heads.AddRange(BitConverter.GetBytes((ushort)_FrameRemarkBytes.Length));//2 bytes
            heads.AddRange(BitConverter.GetBytes(contentLen));//8 bytes
            heads.AddRange(_FrameRemarkBytes);//remark bytes
            return heads.ToArray();
        }
        public ushort GetRemarkLength()
        {
            if (_FrameRemarkBytes.Length > ushort.MaxValue)
            {
                throw new Exception("Maximum remark length is " + ushort.MaxValue);
            }
            return (ushort)_FrameRemarkBytes.Length;
        }
        public long GetContentLength()
        {
            return FrameContent != null ? FrameContent.Length : 0;
        }
        public long GetFrameLength()
        {
            //11: type=1&remarkLen=2&contentLen=8
            return 11 + GetContentLength() + GetRemarkLength();
        }
        public byte GetFrameType()
        {
            return this.FrameType;
        }
        private void Read(DataFrameReadHandler handler, bool includeHead)
        {
            if (_disposed)
            {
                throw new Exception("The IDataFrame disposed");
            }
            long contentLen = FrameContent != null ? FrameContent.Length : 0;
            byte[] headBytes = includeHead ? GetHead(contentLen) : new byte[0];
            if (contentLen < 1)
            {
                Callback(handler, headBytes, true);
                return;
            }
            bool isFrist = true;
            long size = 2048;
            long sPos = 0;
            long sLen = FrameContent.Length;
            bool next = true;
            Exception exception = null;
            while (next)
            {
                long endPos = sPos + size;
                if (endPos >= sLen)
                {
                    size = sLen - sPos;
                }
                byte[] buffer = new byte[size];
                int offset = 0;
                int rlen = buffer.Length;
                if (isFrist)
                {
                    isFrist = false;
                    if (includeHead)
                    {
                        if (buffer.Length < headBytes.Length)
                        {
                            buffer = new byte[buffer.Length + headBytes.Length];
                        }
                        Array.Copy(headBytes, 0, buffer, 0, headBytes.Length);
                        offset = headBytes.Length;
                        rlen = buffer.Length - offset;
                    }
                }
                FrameContent.Position = sPos;
                int len = FrameContent.Read(buffer, offset, rlen);
                if (len < rlen)
                {
                    byte[] temp = new byte[len];
                    Array.Copy(buffer, temp, temp.Length);
                    buffer = temp;
                }
                sPos += len;
                next = sPos < sLen;
                try
                {
                    Callback(handler, buffer, !next);
                }
                catch (Exception ex)
                {
                    next = false;
                    exception = ex;
                }
            }
            if (exception != null)
            {
                throw exception;
            }
        }
        public void ReadAll(DataFrameReadHandler handler)
        {
            Read(handler, true);
        }
        public void ReadContent(DataFrameReadHandler handler)
        {
            Read(handler, false);
        }
        public string GetFrameContentString()
        {
            List<byte> buffer = new List<byte>(1024);
            Read(e => {
                buffer.AddRange(e.Data);
            }, false);
            if (buffer.Count > 0)
            {
                return Encoding.UTF8.GetString(buffer.ToArray());
            }
            return String.Empty;
        }

        #region -- Dispose --
        ~DataFrame()
        {
            Dispose(false);
        }
        private bool _disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            if (disposing)
            {
                if (FrameContent != null)
                {
                    FrameContent.Dispose();
                    FrameContent = null;
                }
                if (OnDisposed != null)
                {
                    try { OnDisposed(this.Handler); }
                    catch { }
                }
            }
        }
        #endregion

    }
    public class DataFrameReceiver : IDataFrameReceiver
    {
        public event DataFrameReceivedHandler OnReceived = null;
        readonly int frameWriteFileLen = 1024 * 1024 * 10;//10M
        readonly long frameMaxLen = 1024L * 1024L * 1024L * 4;//4G
        readonly int frameHeadLen = 11;
        readonly string cacheDir = "";
        bool isFrist = true;
        long currentContentLen = 0;
        long currentRemarkLen = 0;
        long currentReadLen = 0;
        byte[] currentRemarkBytes = new byte[0];
        DataFrame currentDataFrame = null;
        string currentFilePath = "";
        /// <summary>
        /// 读取步骤：0待读取头信息1待读取备注信息2待读取主体内容3读取结束
        /// </summary>
        int currentStep = 0;

        List<byte> _bytes = new List<byte>(2048);

        public DataFrameReceiver() : this(new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "_cache_")))
        {

        }
        public DataFrameReceiver(System.IO.DirectoryInfo dir)
        {
            if (dir == null)
            {
                dir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "_cache_"));
            }
            if (!dir.Exists)
            {
                dir.Create();
            }
            cacheDir = dir.FullName;
        }
        public void Receive(byte[] inputs)
        {
            if (this._bytes.Count > 0)
            {
                this._bytes.AddRange(inputs);
                inputs = this._bytes.ToArray();
                this._bytes.Clear();
            }
            if (isFrist)
            {
                if (inputs.Length < frameHeadLen)
                {
                    this._bytes.AddRange(inputs);
                    return;
                }
                isFrist = false;
            }
            bool next = true;
            while (next)
            {
                switch (currentStep)
                {
                    case 0:

                        currentRemarkLen = BitConverter.ToUInt16(inputs, 1);
                        currentContentLen = BitConverter.ToInt64(inputs, 3);
                        if (currentRemarkLen + currentContentLen > frameMaxLen)
                        {
                            throw new Exception("too long of this data frame");
                        }
                        currentReadLen = 0;
                        currentDataFrame = new DataFrame { FrameType = inputs[0] };
                        currentFilePath = Path.Combine(cacheDir, currentDataFrame.Handler.ToString("N"));
                        currentDataFrame.FrameContent = GetStream(currentContentLen, ref currentFilePath);
                        currentDataFrame.OnDisposed += DataFrameDisposed;
                        if (currentRemarkLen < 1)
                        {
                            currentRemarkBytes = new byte[0];
                        }
                        else
                        {
                            currentRemarkBytes = new byte[currentRemarkLen];
                        }
                        byte[] t0 = new byte[inputs.Length - frameHeadLen];
                        Array.Copy(inputs, frameHeadLen, t0, 0, t0.Length);
                        inputs = t0;
                        currentStep = 1;

                        break;
                    case 1:

                        if (currentRemarkLen > 0)
                        {
                            long needs = currentRemarkLen - currentReadLen;
                            if (needs > inputs.Length)
                            {
                                Array.Copy(inputs, 0, currentRemarkBytes, currentReadLen, inputs.Length);
                                currentReadLen += inputs.Length;
                                next = false;
                            }
                            else if (needs < inputs.Length)
                            {
                                Array.Copy(inputs, 0, currentRemarkBytes, currentReadLen, needs);
                                int dif = (int)(inputs.Length - needs);
                                byte[] t1 = new byte[dif];
                                Array.Copy(inputs, needs, t1, 0, t1.Length);
                                inputs = t1;
                                currentReadLen = 0;
                                currentStep = 2;
                            }
                            else
                            {
                                Array.Copy(inputs, 0, currentRemarkBytes, currentReadLen, inputs.Length);
                                currentReadLen = 0;
                                currentStep = 2;
                                next = false;
                            }
                        }
                        else
                        {
                            currentReadLen = 0;
                            currentStep = 2;
                        }

                        break;
                    case 2:

                        if (currentContentLen > 0)
                        {
                            long needs = currentContentLen - currentReadLen;
                            if (needs > inputs.Length)
                            {
#if NET20
                                this.currentDataFrame.FrameContent.Write(inputs,0,inputs.Length);
#else
                                this.currentDataFrame.FrameContent.Write(inputs);
#endif
                                currentReadLen += inputs.Length;
                                next = false;
                            }
                            else if (needs < inputs.Length)
                            {
                                this.currentDataFrame.FrameContent.Write(inputs, 0, (int)needs);
                                int dif = (int)(inputs.Length - needs);
                                byte[] t2 = new byte[dif];
                                Array.Copy(inputs, needs, t2, 0, t2.Length);
                                inputs = t2;
                                currentReadLen = 0;
                                currentStep = 3;
                            }
                            else
                            {
#if NET20
                                this.currentDataFrame.FrameContent.Write(inputs,0,inputs.Length);
#else
                                this.currentDataFrame.FrameContent.Write(inputs);
#endif
                                inputs = new byte[0];
                                currentReadLen = 0;
                                currentStep = 3;
                            }
                        }
                        else
                        {
                            currentStep = 3;
                        }

                        break;
                    case 3:
                        FireReceived();
                        currentStep = 0;
                        if (inputs.Length < frameHeadLen)
                        {
                            next = false;
                            _bytes.AddRange(inputs);
                        }
                        break;
                }
            }

        }

        private void FireReceived()
        {
            if (OnReceived != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(currentFilePath))
                    {
                        this.currentDataFrame.FrameContent.Close();
                        this.currentDataFrame.FrameContent = new FileStream(currentFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                    }
                    if (currentRemarkLen > 0)
                    {
                        string s = System.Text.Encoding.UTF8.GetString(currentRemarkBytes);
                        this.currentDataFrame.FrameRemark = s;
                    }
                    OnReceived(this.currentDataFrame);
                } catch { }

                this.currentDataFrame.Dispose();
                this.currentDataFrame = null;
            }
        }
        private void DataFrameDisposed(Guid guid)
        {
            string path = Path.Combine(cacheDir, guid.ToString("N"));
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch { }
            }
        }
        private Stream GetStream(long contentLen, ref string path)
        {
            if (contentLen > frameWriteFileLen)
            {
                return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            }
            else
            {
                path = "";
                if (contentLen < 1)
                {
                    return null;
                }
                return new MemoryStream((int)contentLen);
            }
        }
    }

}
