using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.App
{
    internal class Program
    {
        static ZeroWAS.IWebServer<string> webServer;
        static ZeroWAS.RawSocket.Client rawSocketClient;
        static ZeroWAS.WebSocket.Client webSocketClient;
        static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            if (ZeroWASInit())
            {
                RawSocketClientInit();
                WebSocketClientInit();

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

        static void RawSocketClientInit()
        {
            rawSocketClient = new RawSocket.Client(new Uri("http://127.0.0.1:6002/RawSocket?name=user009"));
            rawSocketClient.OnConnectErrorHandler = (e) => {
                Console.WriteLine(e.SocketException.Message);
                e.Retry = true;
            };
            rawSocketClient.OnConnectedHandler = () => {
                Console.WriteLine("RawSocketClient Connected: ClientId=" + rawSocketClient.ClinetId);
                rawSocketClient.SendData(new RawSocket.DataFrame
                {
                    FrameType = 1,
                    FrameContent = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("hello server!"))
                });
            };
            rawSocketClient.OnDisconnectHandler = (e) => {
                Console.WriteLine("RawSocketClient Disconnect({0})", e.Message);
            };
            rawSocketClient.OnReceivedHandler = (e) => {
                if (e.FrameType == 1)
                {
                    Console.WriteLine("RawSocketClient Received: {0}", e.GetFrameContentString());
                }
                else
                {
                    Console.WriteLine("RawSocketClient Received: FrameType={0}&FrameContentLength={1}", e.FrameType, e.FrameContent.Length);
                }
            };
            rawSocketClient.Connect();
            //RawSocketClientWriteLine();
        }
        static void RawSocketClientWriteLine()
        {
            string line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                if (rawSocketClient.IsConnencted)
                {
                    rawSocketClient.SendData(new RawSocket.DataFrame(line, 1));
                }
            }
            RawSocketClientWriteLine();
        }
        static void WebSocketClientInit()
        {
            webSocketClient = new WebSocket.Client(new Uri("ws://127.0.0.1:6002/WebSocket?name=user008"));
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

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            rawSocketClient?.Dispose();
            webSocketClient?.Dispose();
            webServer?.Dispose();
        }

        static bool ZeroWASInit()
        {
            /*site config：*/
            System.IO.FileInfo config = new System.IO.FileInfo(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "site.txt"));
            webServer = new ZeroWAS.WebServer<string>(1000, ZeroWAS.WebApplication.FromFile(config));

            webServer.AddHttpHandler(new ZeroWAS.Http.StaticFileHandler(webServer.WebApp));
            /*http request with missing handler：*/
            webServer.WebApp.OnRequestReceivedHandler = (context) =>
            {
                var req = context.Request;
                var res = context.Response;
                res.StatusCode = ZeroWAS.Http.Status.Not_Found;
                res.End();
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
                    string user = req.QueryString != null ? req.QueryString["name"] : "";
                    if (!string.IsNullOrEmpty(user) && user.Length < 30)
                    {
                        Console.WriteLine("【{0}】{1}:ENTER", wsChannelPath, user);
                        return new ZeroWAS.WebSocket.AuthResult<string> { IsOk = true, User = user, WriteMsg = "Welcome " + user };
                    }
                    return new ZeroWAS.WebSocket.AuthResult<string> { IsOk = false, User = "", WriteMsg = "missing identity." };
                },
                OnDisconnectedHandler = (context, ex) =>
                {
                    Console.WriteLine("【{0}】{1}:OUT", context.Channel.Path, context.User);
                },
                OnTextFrameReceivedHandler = (context, msg) =>
                {
                    string rMsg = string.Format("【{0}】{1}", context.User, msg);
                    context.SendData(rMsg, context.Channel);
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
                OnConnectedHandler = (server, req, wsChannelPath) =>
                {
                    string user = req.QueryString != null ? req.QueryString["name"] : "";
                    if (!string.IsNullOrEmpty(user) && user.Length < 30)
                    {
                        Console.WriteLine("【{0}】{1}:ENTER", wsChannelPath, user);
                        if (user != "user001")
                        {
                            return new ZeroWAS.RawSocket.AuthResult<string> { IsOk = true, User = user, WriteData = "Welcome to the chat room." };
                        }
                        return new ZeroWAS.RawSocket.AuthResult<string> { IsOk = false, User = user, WriteData = "blacklisted." };
                    }
                    return new ZeroWAS.RawSocket.AuthResult<string> { IsOk = false, User = "", WriteData = "missing identity." };
                },
                OnDisconnectedHandler = (context, ex) =>
                {
                    Console.WriteLine("【{0}】{1}:OUT", context.Channel.Path, context.User);
                },
                OnReceivedHandler = (context, data) =>
                {
                    string msg = "";
                    if (data.FrameType == 1)
                    {
                        msg = data.GetFrameContentString();
                    }
                    else
                    {
                        msg = "Type=" + data.FrameType + "&Length=" + data.FrameContent.Length;
                    }
                    Console.WriteLine("【{0}】{1}:{2}", context.Channel.Path, context.User, msg);
                    context.SendData(new ZeroWAS.RawSocket.DataFrame { FrameType = 101 });
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
                //Console.WriteLine("Open=>{0}", webServer.WebApp.HomePageUri);
                //OpenUrl(webServer.WebApp.HomePageUri.ToString());
                return true;
            }
            else
            {
                Console.WriteLine("Error=>{0}",StartException.Message);
                return false;
            }
        }

        static void OpenUrl(string url)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error=>{0}",e.Message);
            }
        }

    }
}
