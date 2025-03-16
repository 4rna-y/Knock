using Knock.Cluster.Services;
using Knock.Transport;
using Knock.Transport.Enum;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebSocket4Net;

namespace Knock.Cluster.Networks
{
    public class WebSocketHandler
    {
        private readonly ILogger logger;
        private readonly IConfiguration config;
        private readonly DataProcesser data;
        private readonly ResponseHandler response;

        private WebSocket client;

        public WebSocketHandler(
            ILogger logger,
            IConfiguration config,
            DataProcesser data,
            ResponseHandler response)
        {
            this.logger = logger;
            this.config = config;
            this.data = data;
            this.response = response;

            client = new WebSocket(config["server-address"]);
            client.LocalEndPoint = IPEndPoint.Parse(config["local-address"]);
            client.NoDelay = true;
            client.Opened += OnOpened;
            client.DataReceived += OnDataReceived;
            client.Error += OnError;
            client.Closed += OnClosed;
        }

        public async Task Start()
        {
            logger.Info("Attempting to connect...");
            await client.OpenAsync();
        }

        public void Send(byte[] packet)
        {
            if (client.State == WebSocketState.Open)
            {
                client.Send(packet, 0, packet.Length);
            }
        }

        public async Task Close()
        {
            if (client.State == WebSocketState.Open)
            {
                await client.CloseAsync();
                client.Dispose();
            }
        }

        private void OnError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            logger.Error(e.Exception);
        }

        private void OnOpened(object sender, EventArgs e)
        {
            logger.Info("Opened");
        }

        private async void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                logger.Info($"Received packet ({e.Data.Length} bytes)");

                byte[] decrypted = data.Decrypt(e.Data);
                DataPacket packet = data.Decode(decrypted);
                
                if (packet.PacketType == PacketTypes.Request)
                {
                    DataPacket responseData = await response.Respond(packet);

                    if (responseData is null) return;

                    byte[] encrypted = data.Encrypt(data.Encode(responseData));
                    client.Send(encrypted, 0, encrypted.Length);
                }
                else
                if (packet.PacketType == PacketTypes.Response)
                {
                    
                }
            }
            catch
            {

            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            logger.Info("Closed");
        }
    }
}
