using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IWebSocketContext<TUser>
    {
        long ClinetId { get; }
        TUser User { get; }
        IHttpRequest UpgradeInfo { get; }
        IWebSocketChannel<TUser> Channel { get; }

        void SendData(string message);
        void SendData(string message, TUser toWSUser);
        void SendData(string message, IWebSocketChannel<TUser> toChannel);
        void SendData(string message, IWebSocketChannel<TUser> toChannel, TUser toWSUser);
        void SendControlFrame(WebSocket.ControlOpcodeEnum opcode);
        void SendControlFrame(WebSocket.ControlOpcodeEnum opcode, TUser toWSUser);
        void SendControlFrame(WebSocket.ControlOpcodeEnum opcode, IWebSocketChannel<TUser> toChannel);
        void SendControlFrame(WebSocket.ControlOpcodeEnum opcode, IWebSocketChannel<TUser> toChannel, TUser toWSUser);
        void SendBinaryData(byte[] binaryData);
        void SendBinaryData(byte[] binaryData, TUser toWSUser);
        void SendBinaryData(byte[] binaryData, IWebSocketChannel<TUser> toChannel);
        void SendBinaryData(byte[] binaryData, IWebSocketChannel<TUser> toChannel, TUser toWSUser);

        void Disconnected();
        void Disconnected(Exception ex);
        void Disconnected(TUser user, Exception ex);

    }
}
