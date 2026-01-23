using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZeroWAS.Common
{
    internal static class TempFile
    {
        public static string GetTempFileName()
        {
            return GetTempFileName(string.Empty);
        }
        public static string GetTempFileName(string prefix)
        {
            string name = Guid.NewGuid().ToString("N") + ".tmp";
            if (!string.IsNullOrEmpty(prefix))
            {
                name = prefix + name;
            }
            return System.IO.Path.Combine(CacheDir.GetDirPath(), name);
        }
    }
}
