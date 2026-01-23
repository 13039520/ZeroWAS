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
        /// 后缀匹配(优先级 1)
        /// </summary>
        string[] Suffixes { get; }
        /// <summary>
        /// 精确匹配(优先级 2)
        /// </summary>
        string ExactPath { get; }
        /// <summary>
        /// 前缀匹配(优先级 3)
        /// </summary>
        string PrefixPath { get; }
        /// <summary>
        /// 正则匹配(优先级 4)
        /// </summary>
        Regex CompiledRegex { get; }
        void ProcessRequest(IHttpContext context);
    }
}
