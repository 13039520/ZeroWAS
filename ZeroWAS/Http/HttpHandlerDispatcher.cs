using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ZeroWAS.Http
{
    /// <summary>
    /// HttpHandler调度器
    /// </summary>
    public class HttpHandlerDispatcher
    {
        private readonly object _lock = new object();

        // 精确匹配表
        private readonly Dictionary<string, IHttpHandler> _exactTable = new Dictionary<string, IHttpHandler>(StringComparer.OrdinalIgnoreCase);

        // 前缀匹配列表
        private readonly List<PrefixHandlerEntry> _prefixList = new List<PrefixHandlerEntry>();

        // 后缀匹配字典：扩展名 -> Handler
        private readonly Dictionary<string, IHttpHandler> _suffixTable = new Dictionary<string, IHttpHandler>(StringComparer.OrdinalIgnoreCase);

        // 正则匹配列表
        private readonly List<IHttpHandler> _regexHandlers = new List<IHttpHandler>();

        private class PrefixHandlerEntry
        {
            public string PrefixPath { get; }
            public IHttpHandler Handler { get; }
            public PrefixHandlerEntry(string prefix, IHttpHandler handler)
            {
                PrefixPath = prefix;
                Handler = handler;
            }
        }

        /// <summary>
        /// 注册 Handler
        /// </summary>
        public void Register(IHttpHandler handler)
        {
            lock (_lock)
            {
                // 1️⃣ Exact
                if (!string.IsNullOrEmpty(handler.ExactPath))
                {
                    _exactTable[handler.ExactPath] = handler;
                }

                // 2️⃣ Prefix
                if (!string.IsNullOrEmpty(handler.PrefixPath))
                {
                    _prefixList.Add(new PrefixHandlerEntry(handler.PrefixPath, handler));
                    _prefixList.Sort((a, b) => b.PrefixPath.Length.CompareTo(a.PrefixPath.Length));
                }

                // 3️⃣ Suffix
                if (handler.Suffixes != null)
                {
                    foreach (var suf in handler.Suffixes)
                    {
                        _suffixTable[suf] = handler;
                    }
                }

                // 4️⃣ Regex
                if (handler.CompiledRegex != null)
                {
                    if ((handler.CompiledRegex.Options & RegexOptions.Compiled) == 0)
                    {
                        throw new InvalidOperationException($"Regex for handler '{handler.Key}' must be compiled using RegexOptions.Compiled.");
                    }
                    _regexHandlers.Add(handler);
                } 
            }
        }

        /// <summary>
        /// Dispatch 请求
        /// </summary>
        public IHttpHandler Dispatch(string pathAndQuery)
        {
            if (string.IsNullOrEmpty(pathAndQuery))
                return null;

            // ----------------------
            // 1 Suffix匹配（只匹配 path，不考虑 query）
            // ----------------------
            int queryIndex = pathAndQuery.IndexOf('?');
            string pathOnly = queryIndex >= 0 ? pathAndQuery.Substring(0, queryIndex) : pathAndQuery;
            int dotIndex = pathOnly.LastIndexOf('.');
            if (dotIndex >= 0 && dotIndex < pathOnly.Length - 1)
            {
                string ext = pathOnly.Substring(dotIndex);
                if (_suffixTable.TryGetValue(ext, out var suffixHandler))
                {
                    return suffixHandler;
                }
            }

            // ----------------------
            // 2 Exact匹配（完整 pathAndQuery）
            // ----------------------
            if (_exactTable.TryGetValue(pathAndQuery, out var exactHandler))
            { return exactHandler; }

            // ----------------------
            // 3 Prefix匹配（完整 pathAndQuery）
            // ----------------------
            foreach (var p in _prefixList)
            {
                if (pathAndQuery.StartsWith(p.PrefixPath, StringComparison.OrdinalIgnoreCase))
                { return p.Handler; }
            }

            // ----------------------
            // 4 Regex匹配（完整 pathAndQuery）
            // ----------------------
            foreach (var h in _regexHandlers)
            {
                if (h.CompiledRegex.IsMatch(pathAndQuery))
                { return h; }
            }

            return null;
        }
    }
}
