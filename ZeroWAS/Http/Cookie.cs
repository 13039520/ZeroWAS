using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Http
{
    public class Cookie
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public TimeSpan? Expires { get; set; }
        public string Path { get; set; }
        public string Domain { get; set; }
        public bool HttpOnly { get; set; }

    }
}
