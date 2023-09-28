using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.WebSocket
{
    public class DataReceiver
    {
        private List<byte> bytes = new List<byte>();
        private DataFrameHeader _header = null;
        private byte[] _extend = new byte[0];
        private byte[] _mask = new byte[0];
        private int _maxLen = 0;
        private long _contentLen = 0;
        /// <summary>
        /// 数据读取状态
        /// <para>0待读取请求头部</para>
        /// <para>1待读取请求主体</para>
        /// <para>2读取完成</para>
        /// <para>3(或其它值)读取错误</para>
        /// </summary>
        int readStep = 0;

        public delegate void DataFrameCallback(ReceivedResult receivedResult);
        private DataFrameCallback dataFrameCallback = null;

        public DataReceiver(DataFrameCallback callback)
        {
            dataFrameCallback = callback;
            _maxLen = 1024 * 1024 * 4;//4M
        }
        public DataReceiver(DataFrameCallback callback, int MaxMessageSize)
        {
            dataFrameCallback = callback;
            if (MaxMessageSize < 1024) { MaxMessageSize = 1024; }
            _maxLen = MaxMessageSize;
        }

        public void Received(byte[] myBytes)
        {
            bytes.AddRange(myBytes);

            bool isBreak = false;
            while (true)
            {

                switch (readStep)
                {
                    case 0:

                        #region -- 读取头信息 --
                        _header = new DataFrameHeader(new byte[] { bytes[0], bytes[1] });
                        bytes.RemoveRange(0, 2);
                        //扩展长度
                        if (_header.Length == 126)
                        {
                            _extend = new byte[] { bytes[0], bytes[1] };
                            bytes.RemoveRange(0, 2);
                        }
                        else if (_header.Length == 127)
                        {
                            _extend = new byte[8];
                            for(int i = 0; i < 8; i++)
                            {
                                _extend[i] = bytes[i];
                            }
                            bytes.RemoveRange(0, 8);
                        }
                        //是否有掩码
                        if (_header.HasMask)
                        {
                            _mask = new byte[4];
                            for (int i = 0; i < 4; i++)
                            {
                                _mask[i] = bytes[i];
                            }
                            bytes.RemoveRange(0, 4);
                        }
                        //消息体长度
                        if (_extend.Length == 0)
                        {
                            _contentLen = _header.Length;
                        }
                        else if (_extend.Length == 2)
                        {
                            _contentLen = (int)_extend[0] * 256 + (int)_extend[1];
                        }
                        else
                        {
                            long len = 0;
                            int n = 1;
                            for (int i = 7; i >= 0; i--)
                            {
                                len += (int)_extend[i] * n;
                                n *= 256;
                            }
                            _contentLen = len;
                        }
                        #endregion

                        if (_contentLen > _maxLen)
                        {
                            try
                            {
                                dataFrameCallback(new ReceivedResult { Data = null, ErrorMessage = "content too long" });
                            }
                            catch { }

                            bytes.Clear();
                            isBreak = true;
                        }
                        else
                        {
                            readStep = 1;//进入主体内容的读取
                        }

                        break;
                    case 1:

                        if (bytes.Count < _contentLen)//内容不足
                        {
                            isBreak = true;
                        }
                        else//内容已经接收完整
                        {
                            readStep = 2;
                        }
                        break;
                    case 2:
                        byte[] _content = new byte[0];
                        if (_contentLen > 0)
                        {
                            _content = bytes.GetRange(0, (int)_contentLen).ToArray();
                            bytes.RemoveRange(0, (int)_contentLen);
                            if (_header.HasMask)
                            {
                                _content = Mask(_content, _mask);
                            }
                        }
                        try
                        {
                            dataFrameCallback(new ReceivedResult { Data = new DataFrame(_header, _content), ErrorMessage = "OK" });
                        }
                        catch { }

                        _contentLen = 0;
                        _extend = new byte[0];
                        _mask = new byte[0];

                        readStep = 0;
                        if (bytes.Count < 4)
                        {
                            isBreak = true;
                        }

                        break;
                }
                if (isBreak)
                {
                    break;
                }
            }
        }

        private byte[] Mask(byte[] data, byte[] mask)
        {
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(data[i] ^ mask[i % 4]);
            }
            return data;
        }


    }

    public class ReceivedResult
    {
        public IWebSocketDataFrame Data { get; set; }
        public string ErrorMessage { get; set; }
    }

}
