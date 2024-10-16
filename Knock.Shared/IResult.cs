using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Shared
{
    public interface IResult
    {
        public bool IsSuccess { get; }
        public int Code { get; }
        public string Message { get; }
        public byte[] GetRawMessage();
        public byte[] ToPacket();
        public IResult FromPacket(byte[] data);
    }
}
