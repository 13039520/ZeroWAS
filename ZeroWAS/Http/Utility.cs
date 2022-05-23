using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace ZeroWAS.Http
{
    public static class Utility
    {
        /// <summary>
        /// 0123456789ABCDEF
        /// </summary>
        private static byte[] _hexVal = new byte[] { 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 65, 66, 67, 68, 69, 70 };
        /// <summary>
        /// 0123456789abcdef
        /// </summary>
        private static byte[] _hexVal2 = new byte[] { 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102 };



        private static byte[] _ByteToHexAsciiValue(byte value, bool useUppercaseHexadecimal)
        {
            byte[] bytes = new byte[2];
            byte[] hexVal = useUppercaseHexadecimal ? _hexVal : _hexVal2;
            if (value > 15)
            {
                byte v = Convert.ToByte(value / 16);
                bytes[1] = hexVal[Convert.ToByte(value % 16)];
                if (v > 15)
                {
                    bytes[0] = hexVal[Convert.ToByte(v % 16)];
                }
                else
                {
                    bytes[0] = hexVal[v];//余
                }
            }
            else
            {
                bytes[0] = hexVal[0];
                bytes[1] = hexVal[value];
            }
            return bytes;
        }
        private static byte _HexAsciiValueToByte(byte lHexCharValue, byte rHexCharValue)
        {
            return Convert.ToByte(_GetHexValue(lHexCharValue) * Math.Pow(16, 1) + _GetHexValue(rHexCharValue) * Math.Pow(16, 0));
        }
        private static byte _GetHexValue(byte hexCharValue)
        {
            byte n = 0;
            while (n < _hexVal.Length)
            {
                if (hexCharValue == _hexVal[n] || hexCharValue == _hexVal2[n])
                {
                    return n;
                }
                n++;
            }
            return 0;
        }
        private static byte[] _UrlEncode(byte[] bytes, bool useUppercaseHexadecimal)
        {
            List<byte> reval = new List<byte>();
            int n = 0;
            while (n < bytes.Length)
            {
                if ((bytes[n] > 64 && bytes[n] < 91) || //A-Z
                     (bytes[n] > 96 && bytes[n] < 123) || //a-z
                     (bytes[n] > 47 && bytes[n] < 58)) //0-9
                {
                    reval.Add(bytes[n]);
                }
                else
                {
                    if (bytes[n] != 32)
                    {
                        reval.Add(37);//"%"
                        reval.AddRange(_ByteToHexAsciiValue(bytes[n], useUppercaseHexadecimal));
                    }
                    else
                    {
                        reval.Add(43);//32(" ")替换为43("+")
                    }
                }
                n++;
            }
            return reval.ToArray();
        }
        private static byte[] _UrlDecode(byte[] bytes)
        {
            List<byte> reval = new List<byte>();
            byte sVal = 37;//"%"
            int n = 0;
            bool flag = false;
            while (n < bytes.Length)
            {
                if(bytes[n] != sVal)
                {
                    if (flag)//遇到特殊字符"%"
                    {
                        //顺位取后两个字节(16进制)
                        byte l = bytes[n];
                        n++;
                        byte r = bytes[n];
                        reval.Add(_HexAsciiValueToByte(l,r));
                        flag = false;
                    }
                    else
                    {
                        if (bytes[n] != 43)
                        {
                            reval.Add(bytes[n]);
                        }
                        else
                        {
                            reval.Add(32);//43("+")替换为32(" ")
                        }
                    }
                }
                else
                {
                    flag = true;
                }
                n++;
            }
            return reval.ToArray();
        }



        public static NameValueCollection ParseQueryString(string query)
        {
            return ParseQueryString(query, Encoding.UTF8);
        }
        public static NameValueCollection ParseQueryString(string query, Encoding encoding)
        {
            NameValueCollection reval = new NameValueCollection();
            if (string.IsNullOrEmpty(query))
            {
                return reval;
            }
            int index = query.IndexOf('?');
            if (index > 0)
            {
                if (index + 1 >= query.Length)
                {
                    return reval;
                }
                query = query.Substring(index + 1);
            }
            string[] frames = query.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string frame in frames)
            {
                int n = frame.IndexOf('=');
                if (n < 1)
                {
                    continue;
                }
                string name = UrlDecode(frame.Substring(0, n), encoding);
                string value = string.Empty;
                if (n + 1 < frame.Length)
                {
                    value = UrlDecode(frame.Substring(n + 1), encoding);
                }
                reval.Add(name, value);
            }
            return reval;
        }
        public static string UrlDecode(string str)
        {
            return UrlDecode(str, Encoding.UTF8);
        }
        public static string UrlDecode(string str, Encoding e)
        {
            return e.GetString(_UrlDecode(e.GetBytes(str)));
        }
        public static string UrlDecode(byte[] bytes)
        {
            return UrlDecode(bytes, Encoding.UTF8);
        }
        public static string UrlDecode(byte[] bytes, Encoding e)
        {
            return e.GetString(_UrlDecode(bytes));
        }
        public static string UrlEncode(string str)
        {
            return UrlEncode(str, Encoding.UTF8);
        }
        public static string UrlEncode(string str,bool useUppercaseHexadecimal)
        {
            return UrlEncode(str, Encoding.UTF8, useUppercaseHexadecimal);
        }
        public static string UrlEncode(string str, Encoding e)
        {
            return UrlEncode(str, e, true);
        }
        public static string UrlEncode(string str, Encoding e, bool useUppercaseHexadecimal)
        {
            return e.GetString(_UrlEncode(e.GetBytes(str), useUppercaseHexadecimal));
        }



    }
}
