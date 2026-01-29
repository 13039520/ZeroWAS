using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using ZeroWAS.RawSocket;

namespace ZeroWAS.App
{
    internal class Program
    {
        static ZeroWAS.IWebServer<string> webServer;
        static ZeroWAS.RawSocket.Client rsClient;
        static ZeroWAS.WebSocket.Client webSocketClient;
        static System.Threading.Timer timer;
        static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            ZeroWAS.CacheDir.SetDirPath(@"D:\_cache_");
            if (ZeroWASInit())
            {
                RSClientInit(1);
                //WebSocketClientInit(5);
                Console.WriteLine("HostName=>{0}", webServer.WebApp.HostName);
                while (true)
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }
            else
            {
                Console.WriteLine("Startup failed");
            }
        }
        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            rsClient?.Dispose();
            webSocketClient?.Dispose();
            webServer?.Dispose();
        }

        static bool isRawSocketClientInit = false;
        static void ReceivedHandle0x01(IRawSocketReceivedMessage msg)
        {
            Console.WriteLine("RS_Client Received: [{0}]{1}", msg.ContentLength, msg.ReadContentAsString(Encoding.UTF8));
        }
        static void ReceivedHandle0xff(IRawSocketReceivedMessage msg)
        {
            string name = msg.Remark;
            if (string.IsNullOrEmpty(name))
            {
                name = "unknown.tmp";
            }
            string path = Path.Combine(@"D:\upload\files", name);
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                msg.CopyContentData(fs);
            }
            Console.WriteLine("RS_Client Received File：[{0}]{1}", msg.ContentLength, path);
        }
        static void RSClientInit(int uid)
        {
            rsClient = new RawSocket.Client(new Uri("http://127.0.0.1:6005/RawSocket?uid=" + uid));
            rsClient.OnConnectErrorHandler = (e) => {
                Console.WriteLine(e.SocketException.Message);
                e.Retry = true;
            };
            rsClient.OnConnectedHandler = () => {
                Console.WriteLine("RS_Client Connected: ClientId=" + rsClient.ClientId);
                rsClient.SendData(new RawSocket.SendMessage(1, "Hello server!", ""));
            };
            rsClient.OnDisconnectHandler = (e) => {
                Console.WriteLine("RS_Client Disconnect({0})", e.Message);
            };
            rsClient.ReceivedHandleRegister(0x01, ReceivedHandle0x01);
            rsClient.ReceivedHandleRegister(0xff, ReceivedHandle0xff);
            rsClient.OnReceivedHandler = (e) => {
                //缺少 Handler 处理程序
                Console.WriteLine("RS_Client Received: Type={0}&ContentLength={1}&RemarkLength={2}", e.Type, e.ContentLength, e.RemarkLength);
            };
            rsClient.Connect();
            RSClientReadLine();
        }
        static void RSClientReadLine()
        {
            string line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                if (rsClient.IsConnected)
                {
                    rsClient.SendData(new RawSocket.SendMessage(1, line, ""));
                }
            }
            RSClientReadLine();
        }
        static void WebSocketClientInit(int uid)
        {
            webSocketClient = new WebSocket.Client(new Uri("ws://127.0.0.1:6005/WebSocket?uid=" + uid));
            webSocketClient.OnConnectErrorHandler = (e) => {
                Console.WriteLine(e.SocketException.Message);
                e.Retry = true;
            };
            webSocketClient.OnConnectedHandler = () => {
                Console.WriteLine("WebSocketClient Connected: ClientId=" + webSocketClient.ClinetId);
                webSocketClient.SendData(new WebSocket.DataFrame("hello server!"));
            };
            webSocketClient.OnDisconnectHandler = (e) => {
                Console.WriteLine("WebSocketClient Disconnect({0})", e.Message);
            };
            webSocketClient.OnReceivedHandler = (e) => {
                if (e.Header.OpCode == 1)
                {
                    Console.WriteLine("WebSocketClient Received: {0}", e.Text);
                }
                else
                {
                    Console.WriteLine("WebSocketClient Received: OpCode={0}&ContentLength={1}", e.Header.OpCode, e.Content.Length);
                }
            };
            webSocketClient.Connect();
            WebSocketClientWriteLine();
        }
        static void WebSocketClientWriteLine()
        {
            string line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                if (webSocketClient.IsConnencted)
                {
                    webSocketClient.SendData(new WebSocket.DataFrame(line));
                }
            }
            WebSocketClientWriteLine();
        }

        static bool ZeroWASInit()
        {
            System.IO.FileInfo config = new System.IO.FileInfo(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "site.txt"));
            webServer = new ZeroWAS.WebServer<string>(3000, ZeroWAS.WebApplication.FromFile(config));
            webServer.WebApp.AddService(typeof(UserService), new UserService());

            webServer.AddHttpHandler(new HttpHandlers.SuffixHttpHandler("ImageHandler",new string[] {".jpg",".jpeg",".png", ".gif", ".webp" }, (context) =>
            {
                System.IO.FileInfo fileInfo = context.Server.GetStaticFile(context.Request.URI.AbsolutePath);
                if (fileInfo != null)
                {
                    //A watermark can be added before output.
                    context.Response.WriteStaticFile(fileInfo);
                }
                else
                {
                    context.Response.StatusCode = Http.Status.Not_Found;
                }
                context.Response.End();
            }));
            //http request with missing handler
            webServer.WebApp.OnRequestReceivedHandler = (context) =>
            {
                if (context.Request.ContentType.IndexOf("multipart/form-data") > -1)
                {
                    Console.WriteLine("test={0}", context.Request.Form["test"]);
                    foreach (var key in context.Request.Files.Keys)
                    {
                        foreach (var file in context.Request.Files[key])
                        {
                            using (System.IO.FileStream fs = new System.IO.FileStream(System.IO.Path.Combine(@"D:\test", file.FileName), System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read))
                            {
                                file.SaveTo(fs);
                            }
                        }
                    }
                }
                else if (context.Request.ContentType.IndexOf("application/x-www-form-urlencoded") > -1)
                {
                    Console.WriteLine("test={0}", context.Request.Form["test"]);
                    Console.WriteLine("test2={0}", context.Request.Form["test2"]);
                }
                context.Response.StatusCode = Http.Status.Not_Found;
                context.Response.End();
            };
            /*result resport of HTTP request：*/
            webServer.WebApp.OnResponseEndHandler = (info) =>
            {
                Console.WriteLine(
                    "【{0}】\r\n{1}\r\n{2}\r\n<--ReceivingTs={3}ms&ReceivedContentLength={4}-->\r\n\r\n{5}\r\n{6}\r\n<--ProcessingTs={7}ms&ReturnedContentLength={8}-->\r\n",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    info.RequestHttpLine,
                    "......",//info.RequestHeader,
                    info.ReceivingTs.TotalMilliseconds,
                    info.ReceivedContentLength,
                    info.ResponseHttpLine,
                    "......",//info.ResponseHeader,
                    info.ProcessingTs.TotalMilliseconds,
                    info.ReturnedContentLength);
            };
            webServer.WebSocketHub.ChannelAdd("/WebSocket", new ZeroWAS.WebSocket.Handlers<string>
            {
                OnConnectedHandler = (server, req, wsChannelPath) =>
                {
                    var userService = server.GetService(typeof(UserService)) as UserService;
                    string userName = userService.GetUserNameByQuery(req);
                    if (string.IsNullOrEmpty(userName))
                    {
                        return new ZeroWAS.WebSocket.AuthResult<string> { IsOk = false, User = "", Content = Encoding.UTF8.GetBytes("missing identity."), ContentOpcode = WebSocket.ContentOpcodeEnum.Text };
                    }
                    Console.WriteLine("【{0}】{1}:ENTER", wsChannelPath, userName);
                    return new ZeroWAS.WebSocket.AuthResult<string> { IsOk = true, User = userName, Content = Encoding.UTF8.GetBytes("Welcome " + userName), ContentOpcode = WebSocket.ContentOpcodeEnum.Text };
                },
                OnDisconnectedHandler = (context, ex) =>
                {
                    Console.WriteLine("【{0}】{1}:OUT", context.Channel.Path, context.User);
                },
                OnTextFrameReceivedHandler = (context, msg) =>
                {
                    string rMsg = string.Format("【{0}】{1}", context.User, msg);
                    context.SendData(new WebSocket.DataFrame(Encoding.UTF8.GetBytes(rMsg), WebSocket.ContentOpcodeEnum.Text), context.Channel);
                    Console.WriteLine("【{0}】{1}", context.Channel.Path, rMsg);
                },
                OnBinaryFrameReceivedHandler = (context, content) =>
                {
                    Console.WriteLine("【{0}】BinaryFrame:User={1}&Length={2}", context.Channel.Path, context.User, content.Length);
                },
                OnContinuationFrameReceivedHandler = (context, content, fin) =>
                {
                    Console.WriteLine("【{0}】ContinuationFrame:User={1}&Length={2}&fin={3}", context.Channel.Path, context.User, content.Length, fin);
                }
            });
            webServer.RawSocketHub.ChannelAdd("/RawSocket", new ZeroWAS.RawSocket.Handlers<string>
            {
                OnConnectedHandler = (server, req, wsChannelPath, clientId) =>
                {
                    var userService = server.GetService(typeof(UserService)) as UserService;
                    string userName = userService.GetUserNameByQuery(req);
                    if (string.IsNullOrEmpty(userName))
                    {
                        return new ZeroWAS.RawSocket.AuthResult<string> { IsOk = false, User = "", MessageContent = null, MessageType = 1, MessageRemark = "missing identity." };
                    }
                    Console.WriteLine("【{0}】{1}:ENTER", wsChannelPath, userName);
                    return new ZeroWAS.RawSocket.AuthResult<string> { IsOk = true, User = userName, MessageContent = Encoding.UTF8.GetBytes("Hello client."), MessageType = 1, MessageRemark = clientId.ToString() };
                },
                OnDisconnectedHandler = (context, ex) =>
                {
                    //var userService = context.GetService(typeof(UserService)) as UserService;
                    Console.WriteLine("【{0}】{1}:OUT", context.Channel.Path, context.User);
                },
                OnReceivedHandler = (context, data) =>
                {
                    string msg = "";
                    if (data.Type == 1)
                    {
                        msg = data.ReadContentAsString(Encoding.UTF8);
                    }
                    else
                    {
                        msg = "Type=" + data.Type;
                    }
                    Console.WriteLine("【{0}】{1}:{2}", context.Channel.Path, context.User, msg);
                    if (data.Type == 1 && msg == "LargeFileTransferTest")
                    {
                        string path = @"D:\LargeFile.zip";
                        IRawSocketSendMessage reply;
                        if (File.Exists(path))
                        {
                            string name = Path.GetFileName(path);
                            reply = new SendMessage(255, new FileStream(path, FileMode.Open), name);
                        }
                        else
                        {
                            reply = new SendMessage(1, "File Not Found.", "");
                        }
                        context.SendData(reply, context.User);
                    }
                }
            });

            Exception StartException = null;
            try
            {
                webServer.ListenStart();
            }
            catch (Exception ex)
            {
                StartException = ex;
            }
            if (StartException == null)
            {
                Console.WriteLine("Listen=>{0}:{1}", webServer.WebApp.ListenIP, webServer.WebApp.ListenPort);
                return true;
            }
            else
            {
                Console.WriteLine("Error=>{0}",StartException.Message);
                return false;
            }
        }

        class UserService
        {
            class User { public int ID { get; set; } public string Name { get; set; } }
            List<User> users = new List<User>(100);
            public UserService()
            {
                for(int i = 1; i < 101; i++)
                {
                    string s = i.ToString();
                    s = s.PadLeft(3, '0');
                    string name = "user" + s;
                    users.Add(new User { ID = i, Name = name });
                }
                users.Add(new User { ID = 999999, Name = "999999" });
            }
            public string GetUserNameByCookie(ZeroWAS.IHttpRequest req)
            {
                return GetUserNameByCookie(req, "uid");
            }
            public string GetUserNameByCookie(ZeroWAS.IHttpRequest req, string cookieName)
            {
                if (req.Cookies == null) { return string.Empty; }
                var cookie = req.Cookies[cookieName];
                if(string.IsNullOrEmpty(cookie)) { return string.Empty; }
                int id;
                if(!int.TryParse(cookie, out id)) { return string.Empty; }
                var o=users.Find(x=>x.ID==id);
                if (o != null) { return o.Name; }
                return string.Empty;
            }
            public string GetUserNameByQuery(ZeroWAS.IHttpRequest req)
            {
                return GetUserNameByQuery(req, "uid");
            }
            public string GetUserNameByQuery(ZeroWAS.IHttpRequest req, string queryKey)
            {
                if (req.QueryString == null) { return string.Empty; }
                var value = req.QueryString[queryKey];
                if (string.IsNullOrEmpty(value)) { return string.Empty; }
                int id;
                if (!int.TryParse(value, out id)) { return string.Empty; }
                var o = users.Find(x => x.ID == id);
                if (o != null) { return o.Name; }
                return string.Empty;
            }
        }

    }
    
}
