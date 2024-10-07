using Fleck;
using Knock.Transport;
using Knock.Transport.Enum;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Services
{
    public class WebSocketService
    {
        private readonly IConfiguration config;
        private readonly DataProcesser processer;
        private readonly LogService log;

        private WebSocketServer server;
        private List<IWebSocketConnection> containers;
        private ConcurrentDictionary<Guid, Func<byte[], Task>> responseEvents;
        private ConcurrentDictionary<Guid, TaskCompletionSource<byte[]>> responseAwaitable;

        public ConcurrentDictionary<Guid, TaskCompletionSource<byte[]>> ResponseAwaitable => responseAwaitable;

        public WebSocketService(
            IConfiguration config,
            DataProcesser processer,
            LogService log) 
        {
            this.config = config;
            this.processer = processer;
            this.log = log;

            containers = new List<IWebSocketConnection>();
            responseEvents = new ConcurrentDictionary<Guid, Func<byte[], Task>>();
            responseAwaitable = new ConcurrentDictionary<Guid, TaskCompletionSource<byte[]>>();
        }

        public int GetConnectedCount() => containers.Count;

        public IWebSocketConnection GetConnection(string address) 
            => containers.FirstOrDefault(
                x => address.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));

        public List<IWebSocketConnection> GetConnections() => containers;

        public void Start()
        {
            server = new WebSocketServer(config["address"]);
            server.ListenerSocket.NoDelay = true;
            server.Start(x =>
            {
                x.OnOpen = () => OnOpen(x);
                x.OnClose = () => OnClose(x);
                x.OnBinary = data => OnBinaryReceived(x, data);
                x.OnError = ex => OnError(x, ex);
            });
        }

        private void OnOpen(IWebSocketConnection info)
        {
            containers.Add(info);
            log.Info($"Connection opened: ||{info.ConnectionInfo.ClientIpAddress}:{info.ConnectionInfo.ClientPort}||")
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private void OnClose(IWebSocketConnection info)
        {
            containers.Remove(info);
            log.Info($"Connection closed: ||{info.ConnectionInfo.ClientIpAddress}:{info.ConnectionInfo.ClientPort}||")
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async void OnBinaryReceived(IWebSocketConnection info, byte[] data)
        {
            DataPacket packet = GetDecryptedData(data);
            if (packet.PacketType == PacketTypes.Request)
            {

            }
            else
            if (packet.PacketType == PacketTypes.Response)
            {
                if (responseAwaitable.ContainsKey(packet.Guid))
                {
                    responseAwaitable.TryGetValue(packet.Guid, out TaskCompletionSource<byte[]> task);
                    task.SetResult(packet.Data);
                    responseAwaitable.TryRemove(packet.Guid, out _);
                    return;
                }
                if (responseEvents.ContainsKey(packet.Guid))
                {
                    await responseEvents[packet.Guid](packet.Data);
                }
            }
        }

        private void OnError(IWebSocketConnection info, Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        public byte[] GetEncryptedData(DataPacket packet)
        {
            byte[] encoded = processer.Encode(packet);
            return processer.Encrypt(encoded);
        }

        public DataPacket GetDecryptedData(byte[] data)
        {
            byte[] decrypted = processer.Decrypt(data);
            return processer.Decode(decrypted);
        }
    }
}
