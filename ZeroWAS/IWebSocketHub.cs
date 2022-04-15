using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IWebSocketHub<TUser>
    {

        bool HasChannel { get; }
        bool ChannelAdd(string path, IWebSocketHandlers<TUser> handler);
        IWebSocketChannel<TUser> ChannelSerach(string wsPath);
        void AddPushTask(WebSocket.PushTask<TUser> task);

        void SendData(string message);
        void SendData(string message, TUser toWSUser);
        void SendData(string message, TUser toWSUser, IWebSocketChannel<TUser> toWSChannel);
        void SendData(string message, IWebSocketChannel<TUser> toWSChannel);
        void SendData(string message, IWebSocketChannel<TUser> toWSChannel, TUser toWSUser);

        void SendBinaryData(byte[] binaryData);
        void SendBinaryData(byte[] binaryData, TUser toWSUser);
        void SendBinaryData(byte[] binaryData, TUser toWSUser, IWebSocketChannel<TUser> toWSChannel);
        void SendBinaryData(byte[] binaryData, IWebSocketChannel<TUser> toWSChannel);
        void SendBinaryData(byte[] binaryData, IWebSocketChannel<TUser> toWSChannel, TUser toWSUser);

        void SendControlFrame(WebSocket.ControlOpcodeEnum opcode);
        void SendControlFrame(WebSocket.ControlOpcodeEnum opcode, TUser toWSUser);
        void SendControlFrame(WebSocket.ControlOpcodeEnum opcode, TUser toWSUser, IWebSocketChannel<TUser> toWSChannel);
        void SendControlFrame(WebSocket.ControlOpcodeEnum opcode, IWebSocketChannel<TUser> toWSChannel);
        void SendControlFrame(WebSocket.ControlOpcodeEnum opcode, IWebSocketChannel<TUser> toWSChannel, TUser toWSUser);

    }
}
