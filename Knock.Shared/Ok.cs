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
        public bool IsSuccess => true;
        public int Code { get; }
        public string Message { get; }

        public Ok() { }

        public Ok(int code, string message)
        {
            Code = code;
            Message = message;
        }

        public byte[] GetRawMessage() => Encoding.UTF8.GetBytes(Message);

        public byte[] ToPacket()
        {
            int code = Code | ((IsSuccess ? 1 : 0) << ((sizeof(int) * 8) - 1));
            byte[] rawMsg = GetRawMessage();
            List<byte> res = new List<byte>();
            
            res.AddRange(BitConverter.GetBytes(code));
            res.AddRange(BitConverter.GetBytes(rawMsg.Length));
            res.AddRange(rawMsg);

            return res.ToArray();
        }

        public IResult FromPacket(byte[] data)
        {
            int code = BitConverter.ToInt32(data, 0);
            if ((code & (1 << ((sizeof(int) * 8) - 1))) != 1)
            {
                return new Ok();
            }
        }
    }
}
