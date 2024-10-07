using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock
{
    public class Error
    {
        public bool IsError { get; }
        public string Message { get; }

        public Error()
        {
            IsError = false;    
            Message = string.Empty;
        }

        public Error(bool isError, string message)
        {
            IsError = isError;
            Message = message;
        }
    }
}
