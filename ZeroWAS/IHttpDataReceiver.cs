using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IHttpDataReceiver
    {
        /// <summary>
        /// 请求数据(允许 set 置空，仅做传递用途)
        /// </summary>
        Http.Request RequestData { get; set; }
        Http.Status ReceiveErrorHttpStatus { get; }
        /// <summary>
        /// 接收解析错误信息
        /// </summary>
        string ReceiveErrorMsg {  get; set; }

        bool Receive(byte[] bytes);
    }
}
