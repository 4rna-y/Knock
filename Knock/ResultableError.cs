using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Knock
{
    public class ResultableError<T> : Error
    {
        public T Result { get; }

        public ResultableError(T res) : base()
        {
            Result = res;    
        }

        public ResultableError(bool isError, string message) : base(isError, message) 
        {

        }

        public static implicit operator T(ResultableError<T> res) => res.Result;
    }
}
