using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZeroWAS.RawSocket
{
    public sealed class SendMessage: IRawSocketSendMessage
    {
        public byte Type { get; }
        public Stream Content { get; }
        public string Remark { get; }
        public SendMessage(byte type, Stream content, string remark)
        {
            this.Type = type;
            this.Content = content;
            this.Remark = remark;
        }
        public SendMessage(byte type, string content, string remark)
        {
            this.Type = type;
            this.Content = new MemoryStream(Encoding.UTF8.GetBytes(content));
            this.Remark = remark;
        }
        public SendMessage(byte type, byte[] content, string remark)
        {
            this.Type = type;
            this.Content = new MemoryStream(content);
            this.Remark = remark;
        }
        public void Dispose()
        {
            if (Content != null)
            {
                Content.Dispose();
            }
#if DEBUG
            Console.WriteLine("[{0}] Dispose.", this.GetType().Name);
#endif
        }
    }
}
