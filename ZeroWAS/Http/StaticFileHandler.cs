using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Http
{

    /* 项目“ZeroWAS (net2.0)”的未合并的更改
    在此之前:
        public class StaticFileHandler : ZeroWAS.Common.HttpHeadler
    在此之后:
        public class StaticFileHandler : HttpHeadler
    */
    public class StaticFileHandler : Http.HttpHeadler
    {
        public StaticFileHandler():
            base("HttpStaticFile", @"^/.+\.(html|htm|css|.js|txt|ico|jpg|jpeg|jpe|png|gif|webp|bmp)\b")
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
