using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IWebApplication
    {
        string ListenIP { get; }
        int ListenPort { get; }
        /// <summary>
        /// 可能为空或包含通配符(为空时建议设置为“*”)
        /// </summary>
        string HostName { get;  }
        IEnumerable<string> CrossOrigins { get; }
        string PFXCertificateFilePath { get; }
        string PFXCertificatePassword { get; }
        System.Security.Cryptography.X509Certificates.X509Certificate2 X509Cer { get; }
        bool UseHttps { get; }
        string ServerName { get;  }
        int HttpConnectionMax { get;  }
        int WSConnectionMax { get;  }
        int HttpLargeFileOutputThreshold { get;  }
        int HttpLargeFileOutputRate { get;  }
        int HttpNoDataActivityHoldTime { get; }
        string[] SiteStaticFileSuffix { get;  }
        string[] SiteDefaultFile { get;  }
        System.IO.DirectoryInfo[] ResourceDirectory { get;  }
        int HttpMaxURILength { get;  }
        long HttpMaxContentLength { get;  }


        Http.Handlers.RequestReceivedHandler OnRequestReceivedHandler { get; set; }
        Http.Handlers.ResponseEndHandler OnResponseEndHandler { get; set; }

        bool IsCrossOrigin(string origin, StringComparison comparison);

        void AddService(Type serviceType, object serviceInstance);
        object GetService(Type serviceType);

        string MapPath(string vPath);
        /// <summary>
        /// 获取静态文件(当路径没有后缀名时视为目录，此时会尝试获取目录默认文件)
        /// </summary>
        /// <param name="vPath"></param>
        /// <returns></returns>
        System.IO.FileInfo GetStaticFile(string vPath);
        
    }
}
