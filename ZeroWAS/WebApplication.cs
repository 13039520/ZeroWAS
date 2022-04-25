using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public class WebApplication : IWebApplication
    {
        private static object _servicesLock = new object();
        private Dictionary<Type, object> _services = new Dictionary<Type, object>();
        /// <summary>
        /// .NET 运行时目录
        /// </summary>
        public readonly string RuntimeDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        private System.Security.Cryptography.X509Certificates.X509Certificate2 _X509Cer = null;
        private int _ListenPort = 5002;
        public string ListenIP { get; set; }
        public int ListenPort { get { return _ListenPort; } set { if (value > 0 && value < 65535) { _ListenPort = value; } } }
        public string HostName { get; set; }
        public string PFXCertificateFilePath { get; set; }
        public string PFXCertificatePassword { get; set; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 X509Cer
        {
            get { return _X509Cer; }
            set { _X509Cer = value; }
        }
        public bool UseHttps { get; set; }
        public Uri HomePageUri { get; set; }
        public string ServerName { get; set; }
        public int HttpConnectionMax { get; set; }
        public int WSConnectionMax { get; set; }
        public int HttpNoDataActivityHoldTime { get; set; }
        public int HttpLargeFileOutputThreshold { get; set; }
        public int HttpLargeFileOutputRate { get; set; }
        public int HttpMaxURILength { get; set; }
        public long HttpMaxContentLength { get; set; }
        public string[] SiteStaticFileSuffix { get; set; }
        public string[] SiteDefaultFile { get; set; }
        public System.IO.DirectoryInfo[] ResourceDirectory { get; set; }

        public Http.Handlers.RequestReceivedHandler OnRequestReceivedHandler { get; set; }
        public Http.Handlers.ResponseEndHandler OnResponseEndHandler { get; set; }

        public WebApplication()
        {
            ListenIP = "127.0.0.1";
            ListenPort = 5002;
            HostName = string.Format("{0}:{1}", ListenIP, ListenPort);
            PFXCertificateFilePath = "";
            PFXCertificatePassword = "";
            ServerName = "ZeroWAS";
            HttpConnectionMax = 5000;
            WSConnectionMax = 5000;
            HttpNoDataActivityHoldTime = 15000;
            HttpLargeFileOutputThreshold = 4194304;
            HttpLargeFileOutputRate = 102400;
            SiteStaticFileSuffix = new string[] {".html",".htm",".css",".js",".xml",".txt", ".ico", ".jpe", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
            SiteDefaultFile = new string[] { "index.html", "index.htm", "default.html", "default.htm" };
            System.IO.DirectoryInfo directory = new System.IO.DirectoryInfo(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "wwwroot"));
            ResourceDirectory =new System.IO.DirectoryInfo[] { directory };
            HttpMaxURILength = short.MaxValue;//默认32767
            HttpMaxContentLength = 4194304;//默认4M
            UseHttps = false;
            HomePageUri = new Uri(string.Format("http://{0}:{1}/{2}", ListenIP, ListenPort, SiteDefaultFile[0]));
        }

        public string MapPath(string vPath)
        {
            if (string.IsNullOrEmpty(vPath))
            {
                vPath = "/";
            }
            if (RuntimeDirectory.IndexOf('\\') > -1)
            {
                if (vPath.IndexOf('/') > -1)
                {
                    vPath = vPath.Replace("/", "\\");
                }
                if (vPath[0] == '\\')
                {
                    vPath = vPath.Substring(1);
                }
                if (string.IsNullOrEmpty(vPath))
                {
                    return ResourceDirectory[0].FullName;
                }
            }
            else
            {
                if (vPath[0] == '/')
                {
                    vPath = vPath.Substring(1);
                }
                if (string.IsNullOrEmpty(vPath))
                {
                    return ResourceDirectory[0].FullName;
                }
            }
            
            int charIndex = vPath.IndexOf('?');
            if (charIndex > 0)
            {
                vPath = vPath.Substring(0, charIndex);
            }
            if (vPath.IndexOf('%') > -1)
            {
                vPath = Http.Utility.UrlDecode(vPath);
            }
            string reval = string.Empty;
            string uriFirstMenu = "";
            if (RuntimeDirectory.IndexOf('\\') > -1)
            {
                charIndex = vPath.IndexOf('\\');
            }
            else
            {
                charIndex = vPath.IndexOf('/');
            }
            if (charIndex > 0)
            {
                uriFirstMenu = vPath.Substring(0, charIndex);
            }

            System.IO.DirectoryInfo[] dirs = ResourceDirectory;
            bool exists = false;
            //优先从虚拟目录查找
            if (dirs.Length > 1 && !string.IsNullOrEmpty(uriFirstMenu))
            {
                for (int i = 1; i < dirs.Length; i++)
                {
                    if (dirs[i].Name.Equals(uriFirstMenu))
                    {
                        exists = true;
                        reval = System.IO.Path.Combine(dirs[i].FullName, vPath.Substring(charIndex + 1));
                        break;
                    }
                }
            }
            if (!exists)
            {
                reval = System.IO.Path.Combine(dirs[0].FullName, vPath);
            }
            return string.IsNullOrEmpty(reval) ? dirs[0].FullName : reval;
        }
        public System.IO.FileInfo GetStaticFile(string vPath)
        {
            System.IO.FileInfo reval = null;
            string path = MapPath(vPath);
            string suffix = System.IO.Path.GetExtension(path);
            //是目录：获取默认文件
            if (string.IsNullOrEmpty(suffix))
            {
                foreach (string df in SiteDefaultFile)
                {
                    string path2 = System.IO.Path.Combine(path, df);
                    if (System.IO.File.Exists(path2))
                    {
                        reval = new System.IO.FileInfo(path2);
                        break;
                    }
                }
            }
            else//是文件
            {
                suffix = suffix.Trim().ToLower();
                if (suffix[0] != '.') { suffix = "." + suffix; }
                bool isStaticFile = false;
                foreach (string s in SiteStaticFileSuffix)
                {
                    if (s == suffix)
                    {
                        isStaticFile = true;
                        break;
                    }
                }
                if (isStaticFile && System.IO.File.Exists(path))
                {
                    reval = new System.IO.FileInfo(path);
                }
            }
            return reval;
        }

        public void AddService(Type serviceType, object serviceInstance)
        {
            if(serviceInstance == null) { return; }
            if (_services.ContainsKey(serviceType)) { return; }
            lock (_servicesLock)
            {
                if (_services.ContainsKey(serviceType)) { return; }
                _services.Add(serviceType, serviceInstance);
            }
        }
        public object GetService(Type serviceType)
        {
            if (_services.ContainsKey(serviceType)) { return _services[serviceType]; }
            return null;
        }


        public static WebApplication FromFile(System.IO.FileInfo file)
        {
            WebApplication reval = new WebApplication();
            if (file == null || !file.Exists) { return reval; }
            string ListenIP = "";
            int ListenPort = 0;
            string ServerName = "";
            string HostName = "";
            string PFXCertificateFilePath = "";
            string PFXCertificatePassword = "";
            int HttpConnectionMax = 0;
            int WSConnectionMax = 0;
            int HttpNoDataActivityHoldTime = 0;
            int HttpLargeFileOutputThreshold = 0;
            int HttpLargeFileOutputRate = 0;
            int HttpMaxURILength = short.MaxValue;//默认32767
            long HttpMaxContentLength = 4194304;//默认4M
            List<string> SiteStaticFileSuffix = new List<string>();
            List<string> SiteDefaultFile = new List<string>();
            System.IO.DirectoryInfo SiteHomeDirectory = null;
            List<System.IO.DirectoryInfo> SiteVirtualDirectory = new List<System.IO.DirectoryInfo>();

            using (var reader = file.OpenText())
            {
                try
                {
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        if (line.Length < 1||line.IndexOf("//")==0)
                        {
                            line = reader.ReadLine();
                            continue;
                        }

                        int index = line.IndexOf('=');
                        if (index < 1 || index + 1 >= line.Length)
                        {
                            line = reader.ReadLine();
                            continue;
                        }
                        string name = line.Substring(0, index).Trim();
                        string value = line.Substring(index + 1).Trim();
                        switch (name)
                        {
                            case "ListenIP":
                                ListenIP = value;
                                break;
                            case "ListenPort":
                                int.TryParse(value, out ListenPort);
                                break;
                            case "HostName":
                                HostName = value;
                                break;
                            case "ServerName":
                                ServerName = value;
                                break;
                            case "HttpConnectionMax":
                                int.TryParse(value, out HttpConnectionMax);
                                break;
                            case "WSConnectionMax":
                                int.TryParse(value, out WSConnectionMax);
                                break;
                            case "HttpNoDataActivityHoldTime":
                                int.TryParse(value, out HttpNoDataActivityHoldTime);
                                break;
                            case "HttpLargeFileOutputThreshold":
                                int.TryParse(value, out HttpLargeFileOutputThreshold);
                                break;
                            case "HttpLargeFileOutputRate":
                                int.TryParse(value, out HttpLargeFileOutputRate);
                                break;
                            case "HttpMaxURILength":
                                int.TryParse(value, out HttpMaxURILength);
                                break;
                            case "HttpMaxContentLength":
                                long.TryParse(value, out HttpMaxContentLength);
                                break;
                            case "SiteStaticFileSuffix":
                                var mc = System.Text.RegularExpressions.Regex.Matches(value, @"\.[0-9a-zA-Z]{1,10}");
                                foreach (System.Text.RegularExpressions.Match m in mc)
                                {
                                    string suffix = m.Value.ToLower();
                                    if (!SiteStaticFileSuffix.Contains(suffix))
                                    {
                                        SiteStaticFileSuffix.Add(suffix);
                                    }
                                }
                                break;
                            case "SiteDefaultFile":
                                var mc2 = System.Text.RegularExpressions.Regex.Matches(value, @"[0-9a-zA-Z]{1,20}\.[0-9a-zA-Z]{1,10}");
                                foreach (System.Text.RegularExpressions.Match m in mc2)
                                {
                                    string fname = m.Value.ToLower();
                                    if (!SiteDefaultFile.Contains(fname))
                                    {
                                        SiteDefaultFile.Add(fname);
                                    }
                                }
                                break;
                            case "SiteHomeDirectory":
                                if (value.IndexOf("@AppBaseDir/") != 0)
                                {
                                    System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(value);
                                    if (dir.Exists)
                                    {
                                        SiteHomeDirectory = dir;
                                    }
                                }
                                else
                                {
                                    value = value.Substring(11);
                                    if (value.Length > 0)
                                    {
                                        value = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, value);
                                        System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(value);
                                        if (dir.Exists)
                                        {
                                            SiteHomeDirectory = dir;
                                        }
                                    }
                                }
                                break;
                            case "SiteVirtualDirectory":
                                if (value.IndexOf("@AppBaseDir/") != 0)
                                {
                                    System.IO.DirectoryInfo dir2 = new System.IO.DirectoryInfo(value);
                                    if (dir2.Exists)
                                    {
                                        bool exists = false;
                                        foreach (System.IO.DirectoryInfo d in SiteVirtualDirectory)
                                        {
                                            if (d.FullName.Equals(dir2.FullName, StringComparison.OrdinalIgnoreCase))
                                            {
                                                exists = true;
                                                break;
                                            }
                                        }
                                        if (!exists)
                                        {
                                            SiteVirtualDirectory.Add(dir2);
                                        }
                                    }
                                }
                                else
                                {
                                    value = value.Substring(11);
                                    if (value.Length > 0)
                                    {
                                        value = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, value);
                                        System.IO.DirectoryInfo dir2 = new System.IO.DirectoryInfo(value);
                                        if (dir2.Exists)
                                        {
                                            bool exists = false;
                                            foreach (System.IO.DirectoryInfo d in SiteVirtualDirectory)
                                            {
                                                if (d.FullName.Equals(dir2.FullName, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    exists = true;
                                                    break;
                                                }
                                            }
                                            if (!exists)
                                            {
                                                SiteVirtualDirectory.Add(dir2);
                                            }
                                        }
                                    }
                                }
                                break;
                            case "SiteMIME":
                                if (!string.IsNullOrEmpty(value)&&value[0]=='.')
                                {
                                    string[] kv = value.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (kv.Length == 2)
                                    {
                                        kv[0] = kv[0].Trim().TrimStart('.');
                                        kv[1] = kv[1].Trim();
                                        if (kv[0].Length > 0 && kv[1].Length > 0)
                                        {
                                            Common.FileMimeTypeMapping.AddOrUpdateMapping(kv[0], kv[1]);
                                        }
                                    }
                                }
                                break;
                            case "PFXCertificateFilePath":
                                string suffix2 = System.IO.Path.GetExtension(value);
                                if (!string.IsNullOrEmpty(suffix2) && suffix2.ToLower() == ".pfx")
                                {
                                    if (value.IndexOf("@AppBaseDir/") != 0)
                                    {
                                        if (System.IO.File.Exists(value))
                                        {
                                            PFXCertificateFilePath = value;
                                        }
                                    }
                                    else
                                    {
                                        value = value.Substring(11);
                                        if (value.Length > 0)
                                        {
                                            value = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, value);
                                            if (System.IO.File.Exists(value))
                                            {
                                                PFXCertificateFilePath = value;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "PFXCertificatePassword":
                                if (!string.IsNullOrEmpty(value))
                                {
                                    PFXCertificatePassword = value;
                                }
                                break;
                        }

                        line = reader.ReadLine();
                    }
                }
                catch { }
            }
            ListenIP = ListenIP.Trim().ToLower();
            
            bool ipIsLocalhostChars = false;
            if (!string.IsNullOrEmpty(ListenIP))
            {
                if (ListenIP == "localhost")
                {
                    ipIsLocalhostChars = true;
                    ListenIP = "127.0.0.1";
                }
                if (ListenIP == "*")
                {
                    ListenIP = "0.0.0.0";
                }
                System.Net.IPAddress iPAddress = null;
                if (!System.Net.IPAddress.TryParse(ListenIP, out iPAddress))
                {
                    ListenIP = "127.0.0.1";
                }
                else
                {
                    ListenIP = iPAddress.ToString();
                }
                reval.ListenIP = ListenIP;
            }
            if(ListenPort>0&& ListenPort< 65535)
            {
                reval.ListenPort = ListenPort;
            }
            if (string.IsNullOrEmpty(HostName))
            {
                if (reval.ListenPort != 80)
                {
                    HostName = string.Format("{0}:{1}", ipIsLocalhostChars ? "localhost" : reval.ListenIP, reval.ListenPort);
                }
                else
                {
                    string name = System.Net.Dns.GetHostName();
                    System.Net.IPAddress[] ipadrlist = System.Net.Dns.GetHostAddresses(name);
                    string ip = "";
                    foreach (System.Net.IPAddress ipa in ipadrlist)
                    {
                        if (ipa.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            ip = ipa.ToString();
                            break;
                        }
                    }
                    HostName = string.Format("{0}", ipIsLocalhostChars ? "localhost" : string.IsNullOrEmpty(ip) ? reval.ListenIP : ip);
                }
            }
            reval.HostName = HostName;
            if (!string.IsNullOrEmpty(ServerName) && System.Text.RegularExpressions.Regex.IsMatch(ServerName, @"^[a-zA-Z0-9_\-]{1,50}$"))
            {
                reval.ServerName = ServerName;
            }
            if (!string.IsNullOrEmpty(PFXCertificateFilePath))
            {
                reval.PFXCertificateFilePath = PFXCertificateFilePath;
            }
            if (!string.IsNullOrEmpty(PFXCertificatePassword))
            {
                reval.PFXCertificatePassword = PFXCertificatePassword;
            }
            else
            {
                reval.PFXCertificateFilePath = "";
            }
            if (!string.IsNullOrEmpty(reval.PFXCertificateFilePath) && !string.IsNullOrEmpty(reval.PFXCertificatePassword) && System.IO.File.Exists(reval.PFXCertificateFilePath))
            {
                try
                {
                    reval.X509Cer = new System.Security.Cryptography.X509Certificates.X509Certificate2(reval.PFXCertificateFilePath, reval.PFXCertificatePassword);
                    reval.UseHttps = true;
                }
                catch { }
            }
            if (HttpConnectionMax > 0)
            {
                reval.HttpConnectionMax = HttpConnectionMax;
            }
            if (WSConnectionMax > 0)
            {
                reval.WSConnectionMax = WSConnectionMax;
            }
            if (HttpNoDataActivityHoldTime > 0)
            {
                reval.HttpNoDataActivityHoldTime = HttpNoDataActivityHoldTime;
            }
            if (HttpLargeFileOutputThreshold > 0)
            {
                reval.HttpLargeFileOutputThreshold = HttpLargeFileOutputThreshold;
            }
            if (HttpMaxContentLength > 0)
            {
                reval.HttpMaxContentLength = HttpMaxContentLength;
            }
            if (HttpMaxURILength > 0)
            {
                reval.HttpMaxURILength = HttpMaxURILength;
            }
            if (HttpLargeFileOutputRate > 0)
            {
                reval.HttpLargeFileOutputRate = HttpLargeFileOutputRate;
            }
            if (SiteStaticFileSuffix.Count > 0)
            {
                reval.SiteStaticFileSuffix = SiteStaticFileSuffix.ToArray();
            }
            if (SiteDefaultFile.Count > 0)
            {
                reval.SiteDefaultFile = SiteDefaultFile.ToArray();
            }
            if (SiteDefaultFile.Count > 0)
            {
                reval.SiteDefaultFile = SiteDefaultFile.ToArray();
            }
            List<System.IO.DirectoryInfo> dirs = new List<System.IO.DirectoryInfo>();
            if (SiteHomeDirectory != null)
            {
                dirs.Add(SiteHomeDirectory);
            }
            else
            {
                dirs.Add(reval.ResourceDirectory[0]);
            }
            foreach(System.IO.DirectoryInfo d in SiteVirtualDirectory)
            {
                bool exists = false;
                foreach(System.IO.DirectoryInfo d2 in dirs)
                {
                    if (d2.FullName.Equals(d.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    dirs.Add(d);
                }
            }
            reval.ResourceDirectory = dirs.ToArray();

            SetHomePageUrl(ref reval);

            return reval;
        }

        private static void SetHomePageUrl(ref WebApplication app)
        {
            StringBuilder url = new StringBuilder(app.UseHttps ? "https" : "http");
            if (app.ListenIP == "*" || app.ListenIP == "0.0.0.0")
            {
                url.Append("://127.0.0.1");
            }
            else
            {
                url.AppendFormat("://{0}", app.ListenIP);
            }
            if (app.ListenPort != 80)
            {
                url.AppendFormat(":{0}", app.ListenPort);
            }
            url.Append("/");
            if(app.SiteDefaultFile.Length > 0)
            {
                url.Append(app.SiteDefaultFile[0]);
            }
            app.HomePageUri=new Uri(url.ToString());
        }

    }
}
