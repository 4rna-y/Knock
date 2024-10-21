using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Shared
{
    public class ResultHelper
    {
        public static IResult FromPacket(byte[] data)
        {
            Span<byte> span = new Span<byte>(data);
            if (BitConverter.ToBoolean(span.Slice(0, 1)))
            {
                return new Ok(BitConverter.ToInt32(span.Slice(1, 2)), Encoding.UTF8.GetString(span.Slice(3)));
            }
            else
            {
                return new Error(BitConverter.ToInt32(span.Slice(1, 2)), Encoding.UTF8.GetString(span.Slice(3)));
            }
        }
    }
}
