using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.WebSocket
{
    public enum ControlOpcodeEnum
    {
        Close = 0,
        Ping = 9,
        Pong = 10
    }
    public enum ContentOpcodeEnum
    {
        Text = 1,
        Binary = 2,
        Close = 0,
        Ping = 9,
        Pong = 10
    }
    public class DataFrame: IWebSocketDataFrame
    {
        DataFrameHeader _header;
        private byte[] _extend = new byte[0];
        private byte[] _mask = new byte[0];
        private byte[] _content = new byte[0];

        public DataFrame(string content) : this(Encoding.UTF8.GetBytes(""+content), ContentOpcodeEnum.Text)
        {
        }
        public DataFrame(byte[] content, ContentOpcodeEnum opcode)
        {
            _content = content;
            int length = _content.Length;

            if (length < 126)
            {
                _extend = new byte[0];
                _header = new DataFrameHeader(true, false, false, false, Convert.ToSByte(opcode), false, length);
            }
            else if (length < 65536)
            {
                _extend = new byte[2];
                _header = new DataFrameHeader(true, false, false, false, Convert.ToSByte(opcode), false, 126);
                _extend[0] = (byte)(length / 256);
                _extend[1] = (byte)(length % 256);
            }
            else
            {
                _extend = new byte[8];
                _header = new DataFrameHeader(true, false, false, false, Convert.ToSByte(opcode), false, 127);

                int left = length;
                int unit = 256;

                for (int i = 7; i > 1; i--)
                {
                    _extend[i] = (byte)(left % unit);
                    left = left / unit;

                    if (left == 0)
                        break;
                }
            }
        }

        public DataFrame(ControlOpcodeEnum opcode)
        {
            _content = new byte[0];
            _extend = new byte[0];
            int length = _content.Length;
            _header = new DataFrameHeader(true, false, false, false, Convert.ToSByte(opcode), false, length);
        }
        public DataFrame(DataFrameHeader header, byte[] content)
        {
            _content = content;
            _extend = new byte[0];
            _header = header;
        }

        public byte[] GetBytes()
        {
            byte[] buffer = new byte[2 + _extend.Length + _mask.Length + _content.Length];
            Buffer.BlockCopy(_header.GetBytes(), 0, buffer, 0, 2);
            Buffer.BlockCopy(_extend, 0, buffer, 2, _extend.Length);
            Buffer.BlockCopy(_mask, 0, buffer, 2 + _extend.Length, _mask.Length);
            Buffer.BlockCopy(_content, 0, buffer, 2 + _extend.Length + _mask.Length, _content.Length);
            return buffer;
        }
        
        public byte[] Content { get { return _content; } }
        public string Text 
        { 
            get 
            {
                if (_header.OpCode != 1)
                {
                    return string.Empty;
                }
                return Encoding.UTF8.GetString(_content); 
            } 
        }
        public DataFrameHeader Header { get { return _header; } }
        public byte[] Payload
        {
            get { return _content; }
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



}
