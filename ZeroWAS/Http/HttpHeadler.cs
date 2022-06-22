using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Http
{
    public abstract class HttpHeadler : IHttpHandler
    {
        private string _Key = "";
        public string Key { get { return _Key; } }
        private string[] _Suffix = new string[0];
        public string[] Suffix { get { return _Suffix; } }
        private string[] _BasePath = new string[] { "/" };
        public string[] BasePath { get { return _BasePath; } }

        public HttpHeadler(IWebApplication app, string handlerKey, string[] suffix, string[] basePath)
        {
            if (!string.IsNullOrEmpty(handlerKey))
            {
                _Key = handlerKey;
            }
            if (basePath != null && basePath.Length > 0)
            {
                _BasePath = basePath;
            }
            if (suffix is null)
            {
                suffix = new string[] { ".*" };
            }
            List<string> temp = new List<string>();
            foreach (string s in suffix)
            {
                if (string.IsNullOrEmpty(s)) { continue; }
                string t = s.Trim().ToLower();
                if (t[0] != '.')
                {
                    t = "." + t;
                }
                if (t == ".*")
                {
                    temp.Clear();
                    temp.Add(t);
                    break;
                }
                temp.Add(t);
            }
            _Suffix = temp.ToArray();

            //有虚拟目录
            if (app.ResourceDirectory.Length > 1)
            {
                temp = new List<string>(_BasePath);
                //第一个是主目录
                for (int i = 1; i < app.ResourceDirectory.Length; i++)
                {
                    string name = "/" + app.ResourceDirectory[i].Name.ToLower() + "/";
                    if (temp.Contains(name)) { continue; }
                    temp.Add(name);
                }
                _BasePath = temp.ToArray();
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
