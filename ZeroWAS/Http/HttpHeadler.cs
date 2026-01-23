using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ZeroWAS.Http
{
    public abstract class HttpHeadler : IHttpHandler
    {
        private string _Key = "";
        public string Key { get { return _Key; } }
        public string[] Suffixes { get; }
        public string ExactPath { get; }
        public string PrefixPath { get; }
        public Regex CompiledRegex { get; }

        public HttpHeadler(string handlerKey, string pathAndQueryPattern, RegexOptions regexOptions= RegexOptions.IgnoreCase)
        {
            if (!string.IsNullOrEmpty(handlerKey))
            {
                _Key = handlerKey;
            }
            if (string.IsNullOrEmpty(pathAndQueryPattern))
            {
                throw new ArgumentException(nameof(pathAndQueryPattern));
            }
            CompiledRegex= new Regex(pathAndQueryPattern, regexOptions | RegexOptions.Compiled);
        }
        public HttpHeadler(string handlerKey, string[] suffixes)
        {
            if (!string.IsNullOrEmpty(handlerKey))
            {
                _Key = handlerKey;
            }
            if(suffixes==null || suffixes.Length < 1)
            {
                throw new ArgumentException(nameof(suffixes));
            }
            Suffixes = suffixes;
        }
        public HttpHeadler(string handlerKey, string path, bool isPrefixPath)
        {
            if (!string.IsNullOrEmpty(handlerKey))
            {
                _Key = handlerKey;
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(nameof(path));
            }
            if (isPrefixPath)
            {
                PrefixPath = path;
            }
            else
            {
                ExactPath = path;
            }
        }

        public virtual void ProcessRequest(IHttpContext context)
        {
            System.IO.FileInfo fileInfo = context.Server.GetStaticFile(context.Request.URI.AbsolutePath);
            if (fileInfo != null)
            {
                context.Response.WriteStaticFile(fileInfo);
            }
            else
            {
                context.Response.StatusCode = Status.Not_Found;
            }
            context.Response.End();
        }
    }
}
