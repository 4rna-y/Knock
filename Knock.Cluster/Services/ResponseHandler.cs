using Knock.Cluster.Models;
using Knock.Shared;
using Knock.Transport;
using Knock.Transport.Enum;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Cluster.Services
{
    public class ResponseHandler
    {
        private readonly ILogger logger;
        private readonly ContainerService container;
        public ResponseHandler(
            ILogger logger,
            ContainerService container) 
        {
            this.logger = logger;
            this.container = container;
        }

        public async Task<DataPacket> Respond(DataPacket request) => request.RequestType switch
        {
            RequestTypes.Status => await ResponseStatus(request),
            RequestTypes.Create => await ResponseCreateServer(request),
            RequestTypes.SetServerPropertyValue => await ResponseSetServerPropertyValue(request),
            RequestTypes.GetServerPropertyValue => await ResponseGetServerPropertyValue(request),
            RequestTypes.Launch => await ResponseLaunch(request),
            _ => null
        };

        private Task<DataPacket> ResponseStatus(DataPacket packet)
        {
            logger.Info("Response status");

            byte[] data = new byte[4];
            data[0] = 0; // Running Container Count
            data[1] = 8; // Container Capacity
            data[2] = 0; // Allocated Memory Amount
            data[3] = 16; // Max Allocatable Memory Amount

            DataPacket dest = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Response)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Status)
                .WithGuid(packet.Guid)
                .WithData(data)
                .Build();

            return Task.FromResult(dest);
        }

        private async Task<DataPacket> ResponseCreateServer(DataPacket packet)
        {
            logger.Info("Response CreateServer");

            ArraySegment<byte> reqData = packet.Data;

            int memoryAmount = packet.GetByteData(0);
            byte serverApp = packet.GetByteData(1);
            Guid guid = packet.GetGuidData(2);
            byte versionSegmentCount = packet.GetByteData(18);
            string versionStr = string.Join('.', packet.GetByteArrayData(19, versionSegmentCount));
            string dlLink = Encoding.UTF8.GetString(reqData.Slice(19 + versionSegmentCount));

            ErrorInfo err = await container.Create(
                x => x.WithId(guid)
                      .WithVersion(versionStr)
                      .WithMemoryAmount(memoryAmount)
                      .WithServerApplication(serverApp)
                      .WithDirectDownloadLink(dlLink));

            DataPacket dest = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Response)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Create)
                .WithGuid(packet.Guid)
                .WithData(new byte[1] { (byte)(err.Success ? 0 : 1) })
                .Build();

            return dest;
        }

        private async Task<DataPacket> ResponseSetServerPropertyValue(DataPacket packet)
        {
            logger.Info("Response SetServerPropertyValue");

            Guid guid = packet.GetGuidData(0);
            int keyLength = packet.GetIntData(16);
            int valueLength = packet.GetIntData(20);
            string key = packet.GetStringData(24, keyLength);
            string value = packet.GetStringData(24 + keyLength, valueLength);

            ErrorInfo err = await container.WriteServerProperty(guid, key, value);

            DataPacket dest = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Response)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.SetServerPropertyValue)
                .WithGuid(packet.Guid)
                .WithData(new byte[1] { (byte)(err.Success ? 0 : 1) })
                .Build();

            return dest;
        }

        private async Task<DataPacket> ResponseGetServerPropertyValue(DataPacket packet)
        {
            logger.Info("Response GetServerPropertyValue");

            Guid guid = packet.GetGuidData(0);
            int keyLength = packet.GetIntData(16);
            string key = packet.GetStringData(20, keyLength);

            string value = await container.GetServerPropertyValue(guid, key);

            DataPacket dest = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Response)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.GetServerPropertyValue)
                .WithGuid(packet.Guid)
                .WithData(Encoding.UTF8.GetBytes(value))
                .Build();

            return dest;
        }

        private async Task<DataPacket> ResponseLaunch(DataPacket packet)
        {
            logger.Info("Response Launch");

            Guid guid = packet.GetGuidData(0);

            IResult res = await container.Launch(guid);

            DataPacket dest = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Response)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Launch)
                .WithGuid(packet.Guid)
                .WithData(res.ToPacket())
                .Build();

            return dest;
        }
    }
}
