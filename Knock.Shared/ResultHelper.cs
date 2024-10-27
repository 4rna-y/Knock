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

            bool result = BitConverter.ToBoolean(span[0..1]);

            if (result)
            {
                return new Ok(BitConverter.ToInt32(span[1..5]), Encoding.UTF8.GetString(span[5..]));
            }
            else
            {
                return new Error(BitConverter.ToInt32(span[1..5]), Encoding.UTF8.GetString(span[5..]));
            }
        }
    }
}
