using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Shared
{
    public class Error : IResult
    {
        public bool IsSuccess { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }

        public Error(int code, string message)
        {
            Code = code;
            Message = message;
        }

        public byte[] GetRawMessage() => Encoding.UTF8.GetBytes(Message);

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
