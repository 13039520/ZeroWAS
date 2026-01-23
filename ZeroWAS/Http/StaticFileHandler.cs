using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Http
{
    public class StaticFileHandler : Http.HttpHeadler
    {
        public StaticFileHandler():
            base("HttpStaticFile", new string[] { ".html", ".htm", ".css", ".js", ".json", ".txt", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".ico" })
        {
            
        }

        public override void ProcessRequest(IHttpContext context)
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
