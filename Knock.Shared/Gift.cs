using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Shared
{
    public class Gift : Ok
    {
        public byte[] RawData { get; set; }
        public Gift()  
        {

        }

        public new byte[] ToPacket()
        {
            List<byte> dest = new List<byte>();

            dest.AddRange(BitConverter.GetBytes(IsSuccess));
            dest.AddRange(BitConverter.GetBytes(Code));
            dest.AddRange(RawData);

            return dest.ToArray();
        }
    }
}
