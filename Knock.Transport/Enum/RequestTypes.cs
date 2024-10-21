using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Transport.Enum
{
    public enum RequestTypes : byte
    {
        Status = 1,
        Create,
        SetServerPropertyValue,
        GetServerPropertyValue,
        Launch
    }
}
