using Knock.Transport.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Transport
{
    public class DataPacket
    {
        private int pos;

        public PacketTypes PacketType { get; set; }
        public DataTypes DataType { get; set; }
        public RequestTypes RequestType { get; set; }
        public Guid Guid { get; set; }
        public int FileNameLength { get; set; }
        public int DataLength { get; set; }
        public string FileName { get; set; }
        public byte[] Data { get; set; }

        public DataPacket(
            PacketTypes packetType, 
            DataTypes dataType,
            RequestTypes requestType,
            Guid guid, 
            int fileNameLength, 
            int dataLength, 
            string fileName,
            byte[] data)
        {
            PacketType = packetType;
            DataType = dataType;
            RequestType = requestType;
            Guid = guid;
            FileNameLength = fileNameLength;
            DataLength = dataLength;
            FileName = fileName;
            Data = data;
        }

        private DataPacket() { }

        public T Get<T>() where T : struct 
        {
            int length = Marshal.SizeOf(typeof(T));
            Span<byte> span = Data.AsSpan().Slice(pos, length);
            pos += length;

            if (typeof(T) == typeof(Guid))
            {
                Guid res = new Guid(span);
                return (T)(object)res;
            }
            
            return Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
        }

        public string Get(int length)
        {
            Span<byte> span = Data.AsSpan().Slice(pos, length);
            pos += length;

            return Encoding.UTF8.GetString(span);
        }

        public byte GetByteData(int index) 
        { 
             return Data[index];
        }

        public int GetIntData(int index)
        {
            return BitConverter.ToInt32(Data.AsSpan().Slice(index, 4));
        }

        public Guid GetGuidData(int index)
        {
            return new Guid(Data.AsSpan().Slice(index, 16));
        }

        public string GetStringData(int index, int length)
        {
            return Encoding.UTF8.GetString(Data.AsSpan().Slice(index, length));
        }

        public byte[] GetByteArrayData(int index, int length)
        {
            return Data.AsSpan().Slice(index, length).ToArray();
        }

        public class Builder
        {
            private DataPacket packet;

            public Builder()
            {
                packet = new DataPacket();
            }

            public Builder WithPacketType(PacketTypes packetType)
            {
                packet.PacketType = packetType;
                return this;
            }

            public Builder WithDataType(DataTypes dataType)
            {
                packet.DataType = dataType; 
                return this;
            }

            public Builder WithRequestType(RequestTypes requestType)
            {
                packet.RequestType = requestType;
                return this;
            }

            public Builder WithGuid(Guid guid)
            {
                packet.Guid = guid;
                return this;
            }

            public Builder WithFileName(string fileName)
            {
                packet.FileName = fileName;
                packet.FileNameLength = Encoding.UTF8.GetByteCount(fileName);
                return this;
            }

            public Builder WithData(byte[] data)
            {
                packet.Data = data;
                packet.DataLength = data.Length;
                return this;
            }

            public DataPacket Build()
            {
                if (packet.Data is null) packet.Data = new byte[0];

                return new DataPacket(
                    packet.PacketType,
                    packet.DataType,
                    packet.RequestType,
                    packet.Guid,
                    packet.FileNameLength,
                    packet.DataLength,
                    packet.FileName,
                    packet.Data);
            }
        }
    }
}
