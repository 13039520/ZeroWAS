using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IWebApplication
    {
        string ListenIP { get; }
        int ListenPort { get; }
        string HostName { get;  }
        string PFXCertificateFilePath { get; }
        string PFXCertificatePassword { get; }
        System.Security.Cryptography.X509Certificates.X509Certificate2 X509Cer { get; }
        bool UseHttps { get; }
        Uri HomePageUri { get; }
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


        string MapPath(string vPath);
        System.IO.FileInfo GetStaticFile(string vPath);
        
    }
}
