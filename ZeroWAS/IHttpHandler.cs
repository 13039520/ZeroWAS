using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ZeroWAS
{
    public interface IHttpHandler
    {
        /// <summary>
        /// 处理程序Key标识
        /// </summary>
        string Key { get; }
        /// <summary>
        /// 正则表达式缓存
        /// </summary>
        System.Text.RegularExpressions.Regex CompiledRegex {  get; }
        void ProcessRequest(IHttpContext context);
    }
}
