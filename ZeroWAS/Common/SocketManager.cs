using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Common
{
    internal static class SocketManager<TUser>
    {
        private static Dictionary<long, WebSocket.Connection<TUser>> wsDic = new Dictionary<long, WebSocket.Connection<TUser>>();
        private static Dictionary<long, IHttpConnection<TUser>> hsDic = new Dictionary<long, IHttpConnection<TUser>>();
        private static Dictionary<long, RawSocket.Connection<TUser>> rsDic = new Dictionary<long, RawSocket.Connection<TUser>>();

        private static object hsDicLock = new object();
        private static object wsDicLock = new object();
        private static object rsDicLock = new object();

        private static IHttpConnection<TUser> TryGetHttpSocket(IHttpConnection<TUser> client)
        {
            return TryGetHttpSocket(client.ClinetId);
        }
        private static IHttpConnection<TUser> TryGetHttpSocket(long clientId)
        {
            IHttpConnection<TUser> reval;
            if(!hsDic.TryGetValue(clientId,out reval))
            {
                reval = null;
            }
            return reval;
        }


        public static void AddHttpSocket(IHttpConnection<TUser> client)
        {
            if (client == null) { throw new ArgumentNullException("client"); }
            if(client.SocketType!= SocketTypeEnum.Http) { throw new ArgumentOutOfRangeException("client.SocketType"); }
            if (TryGetHttpSocket(client) != null) { return; }
            lock (hsDicLock)
            {
                hsDic.Add(client.ClinetId, client);
            }
        }
        public static WebSocket.Connection<TUser> GetWSConnection(IHttpConnection<TUser> client)
        {
            WebSocket.Connection<TUser> reval;
            if (!wsDic.TryGetValue(client.ClinetId,out reval))
            {
                reval = null;
            }
            return reval;
        }
        public static RawSocket.Connection<TUser> GetRSConnection(IHttpConnection<TUser> client)
        {
            RawSocket.Connection<TUser> reval;
            if (!rsDic.TryGetValue(client.ClinetId, out reval))
            {
                reval = null;
            }
            return reval;
        }
        public static void UpgradeWebSocket(IHttpConnection<TUser> socketAccepter, IWebApplication httpServer, IWebSocketChannel<TUser> channel, IHttpRequest httpRequest)
        {
            if (wsDic.ContainsKey(socketAccepter.ClinetId))
            { 
                return;//升级过
            }
            IHttpConnection<TUser> entity = TryGetHttpSocket(socketAccepter.ClinetId);
            if (entity == null) { 
                return;//在列表中不存在
            }

            Remove(socketAccepter);

            entity.SocketType = SocketTypeEnum.WebSocket;
            var ws = new WebSocket.Connection<TUser>(httpServer, socketAccepter, httpRequest, channel);
            lock (wsDicLock)
            {
                wsDic.Add(socketAccepter.ClinetId, ws);
            }
            ws.HandshakeStart();
        }
        public static void UpgradeRawSocket(IHttpConnection<TUser> socketAccepter, IWebApplication httpServer, IRawSocketChannel<TUser> channel, IHttpRequest httpRequest)
        {
            if (rsDic.ContainsKey(socketAccepter.ClinetId))
            {
                return;//升级过
            }
            IHttpConnection<TUser> entity = TryGetHttpSocket(socketAccepter.ClinetId);
            if (entity == null)
            {
                return;//在列表中不存在
            }

            Remove(socketAccepter);
            entity.SocketType = SocketTypeEnum.RawSocket;
            var rs = new RawSocket.Connection<TUser>(httpServer, socketAccepter, httpRequest, channel);
            lock (rsDicLock)
            {
                rsDic.Add(socketAccepter.ClinetId, rs);
            }
            rs.HandshakeStart();
        }
        public static int GetHttpCount()
        {
            return hsDic.Count;
        }
        public static int GetWSCount()
        {
            return wsDic.Count;
        }
        public static int GetRSCount()
        {
            return rsDic.Count;
        }
        public static void DisconnectHttpSocket(DateTime lastActivityTime)
        {
            List<IHttpConnection<TUser>> rs = new List<IHttpConnection<TUser>>();
            lock (hsDicLock)
            {
                foreach (var hs in hsDic.Values)
                {
                    if (hs.LastActivityTime < lastActivityTime)
                    {
                        rs.Add(hs);
                    }
                }
            }
            foreach(IHttpConnection<TUser> httpSocket in rs)
            {
                if (httpSocket != null)
                {
                    httpSocket.Dispose();//会导致移除操作
                }
            }
        }
        public static void DisconnectWSByClientId(long clinetId)
        {
            if (clinetId == 0) { return; }
            WebSocket.Connection<TUser> ws;
            if(!wsDic.TryGetValue(clinetId,out ws)) { return; }
            
            IHttpConnection<TUser> hs = ws.SocketAccepter;
            if (hs == null) { return; }

            hs.Dispose();//会导致移除操作
        }
        public static void DisconnectWSByUser(TUser user)
        {
            List<IHttpConnection<TUser>> hs = new List<IHttpConnection<TUser>>();
            lock (wsDicLock)
            {
                foreach (var ws in wsDic.Values)
                {
                    if (ws.SocketAccepter.User.Equals(user))
                    {
                        hs.Add(ws.SocketAccepter);
                    }
                }
            }
            foreach (IHttpConnection<TUser> httpSocket in hs)
            {
                if (httpSocket != null)
                {
                    httpSocket.Dispose();//会导致移除操作
                }
            }
        }

        public static void DisconnectRSByClientId(long clinetId)
        {
            if (clinetId == 0) { return; }
            RawSocket.Connection<TUser> rs;
            if (!rsDic.TryGetValue(clinetId, out rs)) { return; }

            IHttpConnection<TUser> hs = rs.SocketAccepter;
            if (hs == null) { return; }

            hs.Dispose();//会导致移除操作
        }
        public static void DisconnectRSByUser(TUser user)
        {
            List<IHttpConnection<TUser>> hs = new List<IHttpConnection<TUser>>();
            lock (rsDicLock)
            {
                foreach (var rs in rsDic.Values)
                {
                    if (rs.SocketAccepter.User.Equals(user))
                    {
                        hs.Add(rs.SocketAccepter);
                    }
                }
            }
            foreach (IHttpConnection<TUser> httpSocket in hs)
            {
                if (httpSocket != null)
                {
                    httpSocket.Dispose();//会导致移除操作
                }
            }
        }

        public delegate bool ForeachHeadler(IHttpConnection<TUser> accepter);
        public static void ForeachWS(ForeachHeadler callback)
        {
            lock (wsDicLock)
            {
                foreach(long key in wsDic.Keys)
                {
                    bool flag = true;
                    try
                    {
                        flag = callback(wsDic[key].SocketAccepter);
                    }
                    catch { }
                    if (!flag)
                    {
                        break;
                    }
                }
            }
        }
        public static void ForeachRS(ForeachHeadler callback)
        {
            lock (wsDicLock)
            {
                foreach (long key in rsDic.Keys)
                {
                    bool flag = true;
                    try
                    {
                        flag = callback(rsDic[key].SocketAccepter);
                    }
                    catch { }
                    if (!flag)
                    {
                        break;
                    }
                }
            }
        }

        public static void Remove(IHttpConnection<TUser> client)
        {
            if (client == null) { return; }
            bool removed = false;
            lock (hsDicLock)
            {
                removed = hsDic.Remove(client.ClinetId);
            }
            if (removed) { return; }
            lock (wsDicLock)
            {
                removed = wsDic.Remove(client.ClinetId);
            }
            if (removed) { return; }
            lock (rsDicLock)
            {
                removed = rsDic.Remove(client.ClinetId);
            }
        }
        public static void CleanUp()
        {
            lock (hsDicLock)
            {
                hsDic.Clear();
            }
            lock (wsDicLock)
            {
                wsDic.Clear();
            }
            lock (rsDicLock)
            {
                rsDic.Clear();
            }
        }

    }
}
