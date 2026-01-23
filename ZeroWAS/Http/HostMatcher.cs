using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Http
{
    public static class HostMatcher
    {
        /// <summary>
        /// Host/IP + Port 匹配，支持多层 * 通配符
        /// pattern: example.com, *.example.com, *.*.example.com, 192.168.*.*, 192.168.1.1:8080 等
        /// </summary>
        public static bool Match(string host, string pattern)
        {
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(pattern))
                return false;

            // pattern = "*" 单独处理
            if (pattern == "*")
                return !string.IsNullOrEmpty(host);

            // 分割端口
            string hostName, hostPort = null;
            string patternHost, patternPort = null;

            int i = host.IndexOf(':');
            if (i >= 0)
            {
                hostName = host.Substring(0, i);
                hostPort = host.Substring(i + 1);
            }
            else
            {
                hostName = host;
            }

            i = pattern.IndexOf(':');
            if (i >= 0)
            {
                patternHost = pattern.Substring(0, i);
                patternPort = pattern.Substring(i + 1);
            }
            else
            {
                patternHost = pattern;
            }

            // 端口必须匹配（如果 pattern 指定了端口）
            if (!string.IsNullOrEmpty(patternPort))
            {
                if (hostPort != patternPort)
                    return false;
            }

            // IP 匹配
            bool isIP = true;
            for (int j = 0; j < hostName.Length; j++)
            {
                char c = hostName[j];
                if ((c < '0' || c > '9') && c != '.')
                {
                    isIP = false;
                    break;
                }
            }

            if (isIP)
            {
                // 支持 IPAddress.Any 模式 0.0.0.0
                if (patternHost == "0.0.0.0")
                    return true; // 匹配任意 IPv4 地址

                string[] hostParts = hostName.Split('.');
                string[] patternParts = patternHost.Split('.');

                if (hostParts.Length != patternParts.Length)
                    return false;

                for (int idx = 0; idx < hostParts.Length; idx++)
                {
                    if (patternParts[idx] == "*")
                        continue;

                    if (hostParts[idx] != patternParts[idx])
                        return false;
                }
                return true;
            }
            else
            {
                // 域名匹配（倒序多层 * 支持）
                string[] hostParts = hostName.Split('.');
                string[] patternParts = patternHost.Split('.');

                int hIndex = hostParts.Length - 1;
                int pIndex = patternParts.Length - 1;

                while (hIndex >= 0 && pIndex >= 0)
                {
                    string p = patternParts[pIndex];

                    if (p == "*")
                        return true; // * 匹配任意剩余部分

                    if (!string.Equals(hostParts[hIndex], p, StringComparison.OrdinalIgnoreCase))
                        return false;

                    hIndex--;
                    pIndex--;
                }

                while (pIndex >= 0)
                {
                    if (patternParts[pIndex] != "*")
                        return false;
                    pIndex--;
                }

                return true;
            }
        }
    }
}
