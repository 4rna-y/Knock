using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Models
{
    /// <summary>
    /// Model for storing http response and respond data.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    public class HttpResponseModel<T>
    {
        public HttpStatusCode Code { get; }
        public T Result { get; }

        public HttpResponseModel(HttpStatusCode code, T res)
        {
            Code = code;
            Result = res;
        }
    }
}
