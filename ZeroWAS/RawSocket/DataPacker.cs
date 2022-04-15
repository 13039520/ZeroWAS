using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.RawSocket
{
    /// <summary>
    /// 收发数据包：封包/解包
    /// </summary>
    public class DataPacker
    {
        List<byte> _bytes = new List<byte>();
        /// <summary>
        /// 字节数常量:Length+Type+FrameNum+FrameTotal+FileNameLength+Id
        /// <para>=int+byte+short+short+short+long</para>
        /// <para>=4+1+2+2+2+8=19</para>
        /// </summary>
        public static readonly int HeaderLength = 19;
        public static readonly int MaxPacketLength = 4194304;//4M

        /// <summary>
        /// [静态方法]封包(单个包的负载内容超出4M将抛出异常)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Encode(IRawSocketData data)
        {
            List<byte> bytes = new List<byte>();
            int len = 0;
            bool hasContent = data.Content != null;
            if (hasContent)
            {
                len = data.Content.Length;
            }
            if (len > MaxPacketLength)
            {
                throw new Exception("Data packet length exceeds the upper limit");
            }
            bytes.AddRange(BitConverter.GetBytes(len));
            bytes.Add(data.Type);
            bytes.AddRange(BitConverter.GetBytes(data.FrameNum));
            bytes.AddRange(BitConverter.GetBytes(data.FrameTotal));
            bytes.AddRange(BitConverter.GetBytes(data.FileNameLength));
            bytes.AddRange(BitConverter.GetBytes(data.Id));
            if (len > 0)
            {
                bytes.AddRange(data.Content);
            }
            return bytes.ToArray();
        }

        /// <summary>
        /// [对象方法]解包(单个包的负载内容超出4M将抛出异常)
        /// </summary>
        /// <param name="receiveBuffer"></param>
        /// <returns></returns>
        public List<IRawSocketData> Decode(byte[] receiveBuffer)
        {
            //有缓存数据则联合缓存数据
            if (this._bytes.Count > 0)
            {
                this._bytes.AddRange(receiveBuffer);
                receiveBuffer = this._bytes.ToArray();
                this._bytes.Clear();
            }

            List<IRawSocketData> list = new List<IRawSocketData>();

            if (receiveBuffer.Length < HeaderLength)
            {
                this._bytes.AddRange(receiveBuffer);//缓存起来
                return list;//消息头不足
            }

            while (receiveBuffer != null)
            {
                byte[] myBytes = new byte[4];
                Array.Copy(receiveBuffer, 0, myBytes, 0, 4);
                int msgLen = BitConverter.ToInt32(myBytes, 0);//消息内容长度
                int receiveLen = receiveBuffer.Length;//缓存的内容总长度
                int packLen = HeaderLength + msgLen;//消息包长度
                if (msgLen > MaxPacketLength)
                {
                    throw new Exception("Data packet length exceeds the upper limit");
                }
                if (receiveLen > packLen)//还有剩余
                {
                    if (receiveLen - packLen > 4)
                    {
                        myBytes = new byte[packLen];
                        Array.Copy(receiveBuffer, 4, myBytes, 0, myBytes.Length);
                        list.Add(ToMessage(myBytes, msgLen));
                        //截取剩余长度
                        long offset = packLen;
                        packLen = receiveBuffer.Length - packLen;
                        myBytes = new byte[packLen];
                        Array.Copy(receiveBuffer, offset, myBytes, 0, myBytes.Length);
                        receiveBuffer = myBytes;
                    }
                    else//消息头不足
                    {
                        this._bytes.AddRange(receiveBuffer);//缓存起来
                        receiveBuffer = null;
                    }
                }
                else if (receiveLen < packLen)//内容不足
                {
                    this._bytes.AddRange(receiveBuffer);//缓存起来
                    receiveBuffer = null;
                }
                else//刚好是一条完整的内容
                {
                    myBytes = new byte[packLen - 4];
                    Array.Copy(receiveBuffer, 4, myBytes, 0, myBytes.Length);
                    list.Add(ToMessage(myBytes, msgLen));
                    receiveBuffer = null;
                }

            }

            return list;

        }

        private Data ToMessage(byte[] bytes, int msgLen)
        {
            byte Type = bytes[0];//[0]
            short FrameNum = BitConverter.ToInt16(bytes, 1);//[1,2]
            short FrameTotal = BitConverter.ToInt16(bytes, 3);//[3,4]
            short FileNameLength = BitConverter.ToInt16(bytes, 5);//[5,6]
            long Id = BitConverter.ToInt64(bytes, 7);//[7,8,9,10,11,12,13,14]
            byte[] myBytes = new byte[msgLen];
            Array.Copy(bytes, 15, myBytes, 0, myBytes.Length);
            return new Data
            {
                Type = Type,
                FrameNum = FrameNum,
                FrameTotal = FrameTotal,
                FileNameLength = FileNameLength,
                Id = Id,
                Content = myBytes
            };
        }
    }
}
