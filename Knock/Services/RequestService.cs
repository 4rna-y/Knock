using Discord;
using Fleck;
using Knock.Models;
using Knock.Models.Response;
using Knock.Shared;
using Knock.Transport;
using Knock.Transport.Enum;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Services
{
    public class RequestService
    {
        private readonly IConfiguration config;
        private readonly DataService data;
        private readonly WebSocketService webSocket;

        public RequestService(
            IConfiguration config,
            DataService data,
            WebSocketService webSocket)
        {
            this.config = config;
            this.data = data;
            this.webSocket = webSocket;
        }

        public List<IWebSocketConnection> GetConnections() => webSocket.GetConnections();
        public int GetServerIndexOf(Guid containerId)
        {
            ServerContainer container =
                data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            return this.GetConnections().FindIndex(
                    x => container.StoredLocation.Equals(
                        $"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));
        }

        public async Task<ServerStatus> GetStatus(Guid containerId)
        {
            ServerContainer container =
                data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.None)
                .WithRequestType(RequestTypes.Status)
                .WithGuid(id)
                .Build();
            byte[] req = webSocket.GetEncryptedData(packet);

            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] responsedData = await task.Task;
            return new ServerStatus(responsedData);
        }

        public async Task<bool> CreateServerContainer(Guid containerId)
        {
            ServerContainer container = 
                data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));


            List<byte> version = new List<byte>();
            foreach(string v in container.Version.Split('.'))
            {
                version.Add(byte.Parse(v));
            }
            
            List<byte> dest = new List<byte>();
            dest.Add(byte.Parse(container.MemoryAmount));
            dest.Add((byte)container.Application);
            dest.AddRange(container.Id.ToByteArray());
            dest.Add((byte)version.Count);
            dest.AddRange(version);
            dest.AddRange(Encoding.UTF8.GetBytes(container.DownloadLink));

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Create)
                .WithGuid(id)
                .WithData(dest.ToArray())
                .Build();
            byte[] req = webSocket.GetEncryptedData(packet);
            
            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] res = await task.Task;

            return res[0] == 0;
        }

        public async Task<bool> SetServerPropertyValue(Guid containerId, string key, string value)
        {
            ServerContainer container =
                data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));

            byte[] keyRaw = Encoding.UTF8.GetBytes(key);
            byte[] valueRaw = Encoding.UTF8.GetBytes(value);
            
            List<byte> dest = new List<byte>();
            dest.AddRange(container.Id.ToByteArray());
            dest.AddRange(BitConverter.GetBytes(keyRaw.Length));
            dest.AddRange(BitConverter.GetBytes(valueRaw.Length));
            dest.AddRange(keyRaw);
            dest.AddRange(valueRaw);

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.SetServerPropertyValue)
                .WithGuid(id)
                .WithData(dest.ToArray())
                .Build();
            byte[] req = webSocket.GetEncryptedData(packet);

            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] res = await task.Task;

            return res[0] == 0;
        }

        public async Task<string> GetServerPropertyValue(Guid containerId, string key)
        {
            ServerContainer container =
                data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));

            byte[] keyRaw = Encoding.UTF8.GetBytes(key);

            List<byte> dest = new List<byte>();
            dest.AddRange(container.Id.ToByteArray());
            dest.AddRange(BitConverter.GetBytes(keyRaw.Length));
            dest.AddRange(keyRaw);

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.GetServerPropertyValue)
                .WithGuid(id)
                .WithData(dest.ToArray())
                .Build();
            byte[] req = webSocket.GetEncryptedData(packet);

            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] res = await task.Task;

            return Encoding.UTF8.GetString(res);
        }

        public async Task<IResult> Launch(Guid containerId)
        {
            ServerContainer container =
                data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));

            List<byte> dest = new List<byte>();
            dest.AddRange(container.Id.ToByteArray());

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Launch)
                .WithGuid(id)
                .WithData(dest.ToArray())
                .Build();
            byte[] req = webSocket.GetEncryptedData(packet);

            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] res = await task.Task;

            return ResultHelper.FromPacket(res);
        }

        public async Task<IResult> Stop(Guid containerId)
        {
            ServerContainer container =
                data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));

            List<byte> dest = new List<byte>();
            dest.AddRange(container.Id.ToByteArray());

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Stop)
                .WithGuid(id)
                .WithData(dest.ToArray())
                .Build();
            byte[] req = webSocket.GetEncryptedData(packet);

            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] res = await task.Task;

            return ResultHelper.FromPacket(res);
        }

        public async Task<string> GetLog(Guid containerId)
        {
            ServerContainer container =
               data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));

            List<byte> dest = new List<byte>();
            dest.AddRange(container.Id.ToByteArray());

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Log)
                .WithGuid(id)
                .WithData(dest.ToArray())
                .Build();
            byte[] req = webSocket.GetEncryptedData(packet);

            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] res = await task.Task;

            return Encoding.UTF8.GetString(res);   
        }

        public async Task<List<Guid>> GetOpedIds(Guid containerId)
        {
            ServerContainer container =
               data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));

            List<byte> dest = new List<byte>() { { 0 } };
            dest.AddRange(container.Id.ToByteArray());

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Ops)
                .WithGuid(id)
                .WithData(dest.ToArray())
                .Build();

            byte[] req = webSocket.GetEncryptedData(packet);

            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] res = await task.Task;

            IResult result = ResultHelper.FromPacket(res);
            string joinedIds = result.Message;
            List<string> ids = joinedIds.Split(",").ToList();
            return ids.Select(Guid.Parse).ToList();
        }

        public async Task<IResult> AddOpedIds(Guid containerId, List<Guid> uuids)
        {
            ServerContainer container =
               data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));

            List<byte> uuidsPlaned = uuids.SelectMany(x => x.ToByteArray()).ToList();

            List<byte> dest = new List<byte>() { { 1 } };
            dest.AddRange(container.Id.ToByteArray());
            dest.AddRange(uuidsPlaned);

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Ops)
                .WithGuid(id)
                .WithData(dest.ToArray())
                .Build();

            byte[] req = webSocket.GetEncryptedData(packet);

            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] res = await task.Task;

            return ResultHelper.FromPacket(res);
        }

        public async Task<IResult> RemoveOpedIds(Guid containerId, List<Guid> uuids)
        {
            ServerContainer container =
               data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));

            List<byte> uuidsPlaned = uuids.SelectMany(x => x.ToByteArray()).ToList();

            List<byte> dest = new List<byte>() { { 2 } };
            dest.AddRange(container.Id.ToByteArray());
            dest.AddRange(uuidsPlaned);

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Ops)
                .WithGuid(id)
                .WithData(dest.ToArray())
                .Build();

            byte[] req = webSocket.GetEncryptedData(packet);

            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] res = await task.Task;

            return ResultHelper.FromPacket(res);
        }

        public async Task<List<Guid>> GetWhitelistedIds(Guid containerId)
        {
            ServerContainer container =
               data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));

            List<byte> dest = new List<byte>() { { 0 } };
            dest.AddRange(container.Id.ToByteArray());

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Whitelist)
                .WithGuid(id)
                .WithData(dest.ToArray())
                .Build();

            byte[] req = webSocket.GetEncryptedData(packet);

            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] res = await task.Task;

            IResult result = ResultHelper.FromPacket(res);
            string joinedIds = result.Message;
            if (string.IsNullOrWhiteSpace(joinedIds))
            {
                return new List<Guid>();
            }
            List<string> ids = joinedIds.Split(",").ToList();
            return ids.Select(Guid.Parse).ToList();
        }

        public async Task<IResult> AddWhitelistedIds(Guid containerId, List<Guid> uuids)
        {
            ServerContainer container =
               data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));

            List<byte> uuidsPlaned = uuids.SelectMany(x => x.ToByteArray()).ToList();

            List<byte> dest = new List<byte>() { { 1 } };
            dest.AddRange(container.Id.ToByteArray());
            dest.AddRange(uuidsPlaned);

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Whitelist)
                .WithGuid(id)
                .WithData(dest.ToArray())
                .Build();

            byte[] req = webSocket.GetEncryptedData(packet);

            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] res = await task.Task;

            return ResultHelper.FromPacket(res);
        }

        public async Task<IResult> RemoveWhitelistedIds(Guid containerId, List<Guid> uuids)
        {
            ServerContainer container =
               data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));

            List<byte> uuidsPlaned = uuids.SelectMany(x => x.ToByteArray()).ToList();

            List<byte> dest = new List<byte>() { { 2 } };
            dest.AddRange(container.Id.ToByteArray());
            dest.AddRange(uuidsPlaned);

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.Whitelist)
                .WithGuid(id)
                .WithData(dest.ToArray())
                .Build();

            byte[] req = webSocket.GetEncryptedData(packet);

            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] res = await task.Task;

            return ResultHelper.FromPacket(res);
        }

        public async Task<List<IResult>> SendFiles(Guid containerId, List<Attachment> attachments)
        {
            ServerContainer container =
               data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(containerId));
            IWebSocketConnection connection =
                this.GetConnections().FirstOrDefault(
                    x => container.StoredLocation.Equals($"{x.ConnectionInfo.ClientIpAddress}:{x.ConnectionInfo.ClientPort}"));
        
            List<byte> dest = new List<byte>();

            dest.AddRange(containerId.ToByteArray());
            dest.AddRange(BitConverter.GetBytes(attachments.Count));
            foreach (Attachment file in attachments)
            {
                byte[] nameRaw = Encoding.UTF8.GetBytes(file.Filename);
                byte[] urlRaw = Encoding.UTF8.GetBytes(file.Url);

                dest.AddRange(BitConverter.GetBytes(nameRaw.Length));
                dest.AddRange(BitConverter.GetBytes(urlRaw.Length));
                dest.AddRange(nameRaw);
                dest.AddRange(urlRaw);
            }

            Guid id = Guid.NewGuid();
            DataPacket packet = new DataPacket.Builder()
                .WithPacketType(PacketTypes.Request)
                .WithDataType(DataTypes.Plain)
                .WithRequestType(RequestTypes.SendFile)
                .WithGuid(id)
                .WithData(dest.ToArray())
                .Build();

            byte[] req = webSocket.GetEncryptedData(packet);

            await connection.Send(req);
            webSocket.ResponseAwaitable.TryAdd(id, new TaskCompletionSource<byte[]>());
            webSocket.ResponseAwaitable.TryGetValue(id, out TaskCompletionSource<byte[]> task);
            byte[] res = await task.Task;

            List<IResult> results = new List<IResult>();

            int count = BitConverter.ToInt32(res.AsSpan()[..4]);
            int offset = 4;
            for (int i = 0; i < count; i++)
            {
                int length = BitConverter.ToInt32(res, offset);
                offset += 4;

                byte[] data = new byte[length];
                Array.Copy(res, offset, data, 0, length);
                offset += length;

                IResult result = ResultHelper.FromPacket(data);
                results.Add(result);
            }

            return results;
        }
    }
}
