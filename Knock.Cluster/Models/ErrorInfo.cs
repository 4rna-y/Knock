using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Cluster.Models
{
    public class ErrorInfo
    {
        public bool Success { get; set; }
        public Exception Exception { get; set; }

        public ErrorInfo()
        {
            Success = true;
        }

        public ErrorInfo(Exception ex)
        {
            Exception = ex;
            Success = false;
        }
    }
}
