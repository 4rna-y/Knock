using Fleck;
using Knock.Transport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace Knock.Test
{
    public class Program
    {
        static object lockObj = new object();
        static List<IWebSocketConnection> sockets;

        static void Main(string[] args)
        {
            sockets = new List<IWebSocketConnection>();
            WebSocketServer server = new WebSocketServer("ws://0.0.0.0:54321");
            server.ListenerSocket.NoDelay = true;

            server.Start(x =>
            {
                x.OnOpen = () => OnOpen(x);
                x.OnClose = () => OnClose(x);
                x.OnBinary = msg => OnMessage(x, msg);
                x.OnError = ex => OnError(x, ex);
            });

            while (true)
            {
                string input = Console.ReadLine();
                if (string.Equals(input, "quit")) break;
            }

            sockets.ForEach(socket => socket.Close());
            server.Dispose();
        }

        static void OnError(IWebSocketConnection socket, Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        static async void OnOpen(IWebSocketConnection socket)
        {
            lock (lockObj)
            {
                sockets.Add(socket);
            }

            Console.WriteLine($"Open: {socket.ConnectionInfo.ClientIpAddress} {socket.ConnectionInfo.ClientPort}");
        }

        static void OnClose(IWebSocketConnection socket)
        {
            lock (lockObj)
            {
                sockets.Remove(socket);
            }

            Console.WriteLine($"Close: {socket.ConnectionInfo.ClientIpAddress} {socket.ConnectionInfo.ClientPort}");
        }

        static void OnMessage(IWebSocketConnection socket, byte[] bin)
        {
            Console.WriteLine($"{bin.Length} bytes");
            Console.WriteLine(BitConverter.ToString(bin));

            DataProcesser p = new DataProcesser();
            byte[] a = p.Decrypt(bin);

            DataPacket packet = p.Decode(a);

            Console.WriteLine($"packetType :     {packet.PacketType}");
            Console.WriteLine($"dataType :       {packet.DataType}");
            Console.WriteLine($"guid :           {packet.Guid}");
            Console.WriteLine($"filenameLength : {packet.FileNameLength}");
            Console.WriteLine($"dataLength :     {packet.DataLength}");
            Console.WriteLine(packet.FileName);
            Console.WriteLine(Encoding.UTF8.GetString(packet.Data));
        }

    }
}
