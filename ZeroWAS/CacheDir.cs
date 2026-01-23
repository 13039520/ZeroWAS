using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZeroWAS
{
    /// <summary>
    /// 缓存目录
    /// </summary>
    public static class CacheDir
    {
        private static string dirPath;
        private static string baseDir;
        private static readonly object _lock = new object();
        private static bool isInitialized = false;

        public static bool SetDirPath(string path)
        {
            lock (_lock)
            {
                if (isInitialized)
                {
                    return false; // 已经初始化，不能再设置
                }
                dirPath = path;
                return true;
            }
        }
        internal static string GetDirPath()
        {
            if (!isInitialized)
            {
                lock (_lock)
                {
                    if (!isInitialized)
                    {
                        baseDir = string.IsNullOrEmpty(dirPath) ? Path.Combine(Path.GetTempPath(), "zerowas") : dirPath;
                        if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);
                        isInitialized = true;
                    }
                }
            }
            return baseDir;
        }
    }
}
