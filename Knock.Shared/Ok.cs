using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Shared
{
    public class Ok : IResult
    {
        public bool IsSuccess { get; set; } = true;
        public int Code { get; set; }
        public string Message { get; set; }

        public Ok() { }

        public Ok(int code, string message)
        {
            Code = code;
            Message = message;
        }

        private Ok(bool success, int code, string message)
        {
            IsSuccess = success;
            Code = code;
            Message = message;
        }

        public byte[] GetRawMessage() => Message is null ? new byte[0] : Encoding.UTF8.GetBytes(Message);

        public byte[] ToPacket()
        {
            byte[] rawMsg = GetRawMessage();
            List<byte> dest = new List<byte>();
            
            dest.AddRange(BitConverter.GetBytes(IsSuccess));
            dest.AddRange(BitConverter.GetBytes(Code));
            dest.AddRange(rawMsg);

            return dest.ToArray();
        }
    }
}
