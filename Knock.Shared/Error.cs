using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Shared
{
    public class Error : IResult
    {
        public bool IsSuccess => false;
        public int Code { get; }
        public string Message { get; }

        public Error(int code, string message)
        {
            Code = code;
            Message = message;
        }

        public byte[] GetRawMessage() => Encoding.UTF8.GetBytes(Message);
    }
}
