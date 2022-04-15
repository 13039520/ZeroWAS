using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Http
{
    public class StaticFileHandler : IHttpHandler
    {
        private string _Key = "HttpStaticFile";
        public string Key { get { return _Key; } }
        private string[] _Suffix = new string[] { ".html", ".htm", ".css", ".js", ".txt", ".ico", ".jpg", ".jpeg", ".jpe", ".png", ".gif", ".webp", ".bmp" };
        public string[] Suffix { get { return _Suffix; } }
        private string[] _BasePath = new string[] { "/" };
        public string[] BasePath { get { return _BasePath; } }

        public StaticFileHandler() { }
        public StaticFileHandler(IWebApplication app)
        {
            string[] suffix = app.SiteStaticFileSuffix;
            if (suffix != null && suffix.Length > 0)
            {
                List<string> temp = new List<string>(_Suffix);
                foreach(string s in suffix)
                {
                    if (string.IsNullOrEmpty(s)) { continue; }
                    string t = s.Trim().ToLower();
                    if (t[0] != '.')
                    {
                        t = "." + t;
                    }
                    if (temp.Contains(t)) { continue; }
                    temp.Add(t);
                }
                _Suffix = temp.ToArray();
            }
            
            //有虚拟目录
            if (app.ResourceDirectory.Length > 1)
            {
                List<string> temp = new List<string>(_BasePath);
                //第一个是主目录
                for (int i = 1; i < app.ResourceDirectory.Length; i++)
                {
                    string name = "/"+app.ResourceDirectory[i].Name.ToLower()+"/";
                    if (temp.Contains(name)) { continue; }
                    temp.Add(name);
                }
                _BasePath = temp.ToArray();
            }
        }

        public void ProcessRequest(IHttpContext context)
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
