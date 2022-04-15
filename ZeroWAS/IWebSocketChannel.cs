using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS
{
    public interface IWebSocketChannel<TUser>
    {
        string Path { get; }
        IWebSocketHandlers<TUser> Handlers { get; set; }

        void AddPushTask(WebSocket.PushTask<TUser> task);

        void SendToCurrentChannel(string message);
        void SendToCurrentChannel(string message, TUser toUser);

        void SendToHub(string message);
        void SendToHub(string message, TUser toUser);
        void SendToHub(string message, TUser toUser, IWebSocketChannel<TUser> toChannel);
        void SendToHub(string message, IWebSocketChannel<TUser> toChannel);
        void SendToHub(string message, IWebSocketChannel<TUser> toChannel, TUser toUser);

        void SendBinaryDataToCurrentChannel(byte[] binaryData);
        void SendBinaryDataToCurrentChannel(byte[] binaryData, TUser toUser);

        void SendBinaryDataToHub(byte[] binaryData);
        void SendBinaryDataToHub(byte[] binaryData, TUser toUser);
        void SendBinaryDataToHub(byte[] binaryData, TUser toUser, IWebSocketChannel<TUser> toChannel);
        void SendBinaryDataToHub(byte[] binaryData, IWebSocketChannel<TUser> toChannel);
        void SendBinaryDataToHub(byte[] binaryData, IWebSocketChannel<TUser> toChannel, TUser toUser);

        void SendControlFrameToCurrentChannel(WebSocket.ControlOpcodeEnum opcode);
        void SendControlFrameToCurrentChannel(WebSocket.ControlOpcodeEnum opcode, TUser toUser);

        void SendControlFrameToHub(WebSocket.ControlOpcodeEnum opcode);
        void SendControlFrameToHub(WebSocket.ControlOpcodeEnum opcode, TUser toUser);
        void SendControlFrameToHub(WebSocket.ControlOpcodeEnum opcode, TUser toUser, IWebSocketChannel<TUser> toChannel);
        void SendControlFrameToHub(WebSocket.ControlOpcodeEnum opcode, IWebSocketChannel<TUser> toChannel);
        void SendControlFrameToHub(WebSocket.ControlOpcodeEnum opcode, IWebSocketChannel<TUser> toChannel, TUser toUser);


    }
}
