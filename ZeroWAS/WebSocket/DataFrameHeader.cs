using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.WebSocket
{
    public class DataFrameHeader
    {
        private bool _fin;
        private bool _rsv1;
        private bool _rsv2;
        private bool _rsv3;
        private sbyte _opcode;
        private bool _maskcode;
        private sbyte _payloadlength;

        /// <summary>
        /// 如果是true，表示这是消息（message）的最后一个分片（fragment），如果是false，表示不是消息（message）的最后一个分片（fragment）。
        /// </summary>
        public bool FIN { get { return _fin; } }
        /// <summary>
        /// 一般情况下为false
        /// </summary>
        public bool RSV1 { get { return _rsv1; } }
        /// <summary>
        /// 一般情况下为false
        /// </summary>
        public bool RSV2 { get { return _rsv2; } }
        /// <summary>
        /// 一般情况下为false
        /// </summary>
        public bool RSV3 { get { return _rsv3; } }
        /// <summary>
        /// 操作代码，Opcode的值决定了应该如何解析后续的数据载荷（data payload）。如果操作代码是不认识的，那么接收端应该断开连接（fail the connection）。
        /// <para>可选的操作代码如下：</para>
        /// <para>%x0：表示一个延续帧。当Opcode为0时，表示本次数据传输采用了数据分片，当前收到的数据帧为其中一个数据分片。</para>
        /// <para>%x1：表示这是一个文本帧（frame）</para>
        /// <para>%x2：表示这是一个二进制帧（frame）</para>
        /// <para>%x3-7：保留的操作代码，用于后续定义的非控制帧。</para>
        /// <para>%x8：表示连接断开。</para>
        /// <para>%x9：表示这是一个ping操作。</para>
        /// <para>%xA：表示这是一个pong操作。</para>
        /// <para>%xB-F：保留的操作代码，用于后续定义的控制帧。</para>
        /// </summary>
        public sbyte OpCode { get { return _opcode; } }
        /// <summary>
        /// 表示是否要对数据载荷进行掩码操作。从客户端向服务端发送数据时，需要对数据进行掩码操作；从服务端向客户端发送数据时，不需要对数据进行掩码操作。
        /// <para>如果服务端接收到的数据没有进行过掩码操作，服务端需要断开连接。</para>
        /// <para>如果是true，那么在Masking-key中会定义一个掩码键（masking key），并用这个掩码键来对数据载荷进行反掩码。所有客户端发送到服务端的数据帧，Mask都是true。</para>
        /// </summary>
        public bool HasMask { get { return _maskcode; } }
        /// <summary>
        /// 数据载荷的长度，单位是字节。为7位，或7+16位，或1+64位。
        /// <para>假设数Payload length === x，如果</para>
        /// <para>x为0 ~126：数据的长度为x字节。</para>
        /// <para>x为126：后续2个字节代表一个16位的无符号整数，该无符号整数的值为数据的长度。</para>
        /// <para>x为127：后续8个字节代表一个64位的无符号整数（最高位为0），该无符号整数的值为数据的长度。</para>
        /// <para>此外，如果payload length占用了多个字节的话，payload length的二进制表达采用网络序（big endian，重要的位在前）。</para>
        /// </summary>
        public sbyte Length { get { return _payloadlength; } }

        public DataFrameHeader(byte[] buffer)
        {
            if(buffer.Length<2)
                throw new Exception("无效的数据头.");

            //第一个字节
            _fin = (buffer[0] & 0x80) == 0x80;
            _rsv1 = (buffer[0] & 0x40) == 0x40;
            _rsv2 = (buffer[0] & 0x20) == 0x20;
            _rsv3 = (buffer[0] & 0x10) == 0x10;
            _opcode = (sbyte)(buffer[0] & 0x0f);

            //第二个字节
            _maskcode = (buffer[1] & 0x80) == 0x80;
            _payloadlength = (sbyte)(buffer[1] & 0x7f);

        }

        //发送封装数据
        public DataFrameHeader(bool fin,bool rsv1,bool rsv2,bool rsv3,sbyte opcode,bool hasmask,int length)
        {
            _fin = fin;
            _rsv1 = rsv1;
            _rsv2 = rsv2;
            _rsv3 = rsv3;
            _opcode = opcode;
            //第二个字节
            _maskcode = hasmask;
            _payloadlength = (sbyte)length;
        }

        //返回帧头字节
        public byte[] GetBytes()
        {
            byte[] buffer = new byte[2]{0,0};

            if (_fin) buffer[0] ^= 0x80;
            if (_rsv1) buffer[0] ^= 0x40;
            if (_rsv2) buffer[0] ^= 0x20;
            if (_rsv3) buffer[0] ^= 0x10;

            buffer[0] ^= (byte)_opcode;

            if (_maskcode) buffer[1] ^= 0x80;

            buffer[1] ^= (byte)_payloadlength;

            return buffer;
        }
    }
}
