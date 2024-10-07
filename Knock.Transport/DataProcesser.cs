using Knock.Transport.Enum;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Knock.Transport
{
    public class DataProcesser
    {
        private readonly byte[] knkey;
        public DataProcesser() 
        {
            knkey = Encoding.UTF8.GetBytes(File.ReadAllText("aes.knkey"));
        }

        public byte[] Encode(DataPacket packet)
        {
            byte packetType = (byte)packet.PacketType;
            byte dataType = (byte)packet.DataType;
            byte requestType = (byte)packet.RequestType;
            byte[] guid = packet.Guid.ToByteArray();
            byte[] fileNameLength = BitConverter.GetBytes(packet.FileNameLength);
            byte[] dataLength = BitConverter.GetBytes(packet.DataLength);
            byte[] fileName;
            if (!string.IsNullOrWhiteSpace(packet.FileName))
                fileName = Encoding.UTF8.GetBytes(packet.FileName);
            else
                fileName = new byte[0];

            byte[] data = packet.Data;

            byte[] dest = new byte[ // 26 + n + m
                sizeof(byte) + // 1 
                sizeof(byte) + // 2
                sizeof(byte) + // 3
                guid.Length +  // 19
                fileNameLength.Length + // 23 
                dataLength.Length + // 27
                fileName.Length + // n 
                data.Length]; // m

            dest[0] = packetType;
            dest[1] = dataType;
            dest[2] = requestType;
            for (int i = 3,  j = 0; j < guid.Length;           i++, j++) dest[i] = guid[j];
            for (int i = 19, j = 0; j < fileNameLength.Length; i++, j++) dest[i] = fileNameLength[j];
            for (int i = 23, j = 0; j < dataLength.Length;     i++, j++) dest[i] = dataLength[j];
            for (int i = 27, j = 0; j < fileName.Length;       i++, j++) dest[i] = fileName[j];
            for (int i = 27 + fileName.Length, j = 0; j < data.Length; i++, j++) dest[i] = data[j];

            return dest;
        }

        public DataPacket Decode(byte[] packet)
        {
            Span<byte> raw = packet;
            byte packetType = packet[0];
            byte dataType = packet[1];
            byte requestType = packet[2];
            Guid guid = new Guid(raw.Slice(3, 16));
            int fileNameLength = BitConverter.ToInt32(raw.Slice(19, 4));
            int dataLength = BitConverter.ToInt32(raw.Slice(23, 4));
            string fileName = Encoding.UTF8.GetString(raw.Slice(27, fileNameLength));
            Span<byte> data = raw.Slice(27 + fileNameLength, dataLength);

            return new DataPacket(
                (PacketTypes)packetType, (DataTypes)dataType, (RequestTypes)requestType, guid, fileNameLength, dataLength, fileName, data.ToArray());
        }

        public byte[] Encrypt(string value)
        {
            return Encrypt(Encoding.UTF8.GetBytes(value));
        }

        public byte[] Encrypt(byte[] value)
        {
            byte[] initVec = new byte[16];
            byte[] key;

            using (SHA256 sha = SHA256.Create())
            {
                key = sha.ComputeHash(knkey);
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = initVec;

                using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using MemoryStream ms = new MemoryStream();
                using CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
               
                cs.Write(value, 0, value.Length);
                cs.FlushFinalBlock();

                return ms.ToArray();
            }
        }

        public byte[] Decrypt(byte[] value)
        {
            byte[] iv = new byte[16];
            byte[] key;

            using (var sha256 = SHA256.Create())
            {
                key = sha256.ComputeHash(knkey);
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using MemoryStream ms = new MemoryStream(value);
                using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using MemoryStream dest = new MemoryStream();

                cs.CopyTo(dest);
                return dest.ToArray();
            }
        }
    }
}
