using Knock.Cluster.Models;
using Knock.Shared;
using Knock.Transport;
using Knock.Transport.Enum;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration config;
        private readonly ILogger logger;
        private readonly ContainerService container;
        public ResponseHandler(
            IConfiguration config,
            ILogger logger,
            ContainerService container) 
        {
            this.config = config;
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
            RequestTypes.Stop => await ResponseStop(request),
            RequestTypes.Log => await ResponseLog(request),
            RequestTypes.Ops => await ResponseOps(request),
            RequestTypes.Whitelist => await ResponseWhitelist(request),
            RequestTypes.SendFile => await ResponseFileReceived(request),
            _ => null
        };

        private Task<DataPacket> ResponseStatus(DataPacket packet)
        {
            logger.Info("Response status");

            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(container.GetContainerCount())); // Running Container Count
            data.AddRange(BitConverter.GetBytes(int.Parse(config["max-container-count"]))); // Container Capacity
            data.AddRange(BitConverter.GetBytes(container.GetUsedMemory())); // Allocated Memory Amount
            data.AddRange(BitConverter.GetBytes(int.Parse(config["memory-amount"]))); // Max Allocatable Memory Amount

            DataPacket dest = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Response)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Status)
                .WithGuid(packet.Guid)
                .WithData(data.ToArray())
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

        private async Task<DataPacket> ResponseStop(DataPacket packet)
        {
            logger.Info("Response Stop");

            Guid guid = packet.GetGuidData(0);

            IResult res = await container.Stop(guid);

            DataPacket dest = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Response)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Stop)
                .WithGuid(packet.Guid)
                .WithData(res.ToPacket())
                .Build();

            return dest;
        }

        private async Task<DataPacket> ResponseLog(DataPacket packet)
        {
            logger.Info("Response Log");

            Guid guid = packet.GetGuidData(0);

            string log = await container.GetLog(guid);

            DataPacket dest = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Response)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Stop)
                .WithGuid(packet.Guid)
                .WithData(Encoding.UTF8.GetBytes(log))
                .Build();

            return dest;
        }

        private async Task<DataPacket> ResponseOps(DataPacket packet)
        {
            logger.Info("Response Ops");

            byte type = packet.GetByteData(0);
            Guid guid = packet.GetGuidData(1);
            List<Guid> ids = new List<Guid>();
            int listLength = packet.Data.Length - 17;
            int amount = listLength / 16;

            for (int i = 0; i < amount; i++)
            {
                ids.Add(packet.GetGuidData(1 + (16 * (i + 1))));
            }

            IResult res = null;

            if (type == 0) res = await container.GetOpedIds(guid);
            if (type == 1) res = await container.AddOpedIds(guid, ids);
            if (type == 2) res = await container.RemoveOpedIds(guid, ids);

            DataPacket dest = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Response)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Ops)
                .WithGuid(packet.Guid)
                .WithData(res.ToPacket())
                .Build();
            
            return dest;
        }

        private async Task<DataPacket> ResponseWhitelist(DataPacket packet)
        {
            logger.Info("Response Whitelist");

            byte type = packet.GetByteData(0);
            Guid guid = packet.GetGuidData(1);
            List<Guid> ids = new List<Guid>();
            int listLength = packet.Data.Length - 17;
            int amount = listLength / 16;

            for (int i = 0; i < amount; i++)
            {
                ids.Add(packet.GetGuidData(1 + (16 * (i + 1))));
            }

            IResult res = null;

            if (type == 0) res = await container.GetWhitelistedIds(guid);
            if (type == 1) res = await container.AddWhitelistedIds(guid, ids);
            if (type == 2) res = await container.RemoveWhitelistedIds(guid, ids);

            DataPacket dest = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Response)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Whitelist)
                .WithGuid(packet.Guid)
                .WithData(res.ToPacket())
                .Build();

            return dest;
        }

        private async Task<DataPacket> ResponseFileReceived(DataPacket packet)
        {
            logger.Info("Response File");

            List<byte> destData = new List<byte>();
            
            Guid containerId = packet.Get<Guid>();
            int fileCount = packet.Get<int>();
            destData.AddRange(BitConverter.GetBytes(fileCount));

            for (int i = 0; i < fileCount; i++)
            {
                int nameLength = packet.Get<int>();
                int urlLength = packet.Get<int>();
                string name = packet.Get(nameLength);
                string url = packet.Get(urlLength);

                IResult res = await container.AttachFile(containerId, new FileAttachment(name, url));
                byte[] data = res.ToPacket();
                destData.AddRange(BitConverter.GetBytes(data.Length));
                destData.AddRange(data);
            }

            DataPacket dest = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Response)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.SendFile)
                .WithGuid(packet.Guid)
                .WithData(destData.ToArray())
                .Build();

            return dest;
        }
    }
}
