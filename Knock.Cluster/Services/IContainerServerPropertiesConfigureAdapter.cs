using Knock.Cluster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Cluster.Services
{
    public interface IContainerServerPropertiesConfigureAdapter
    {
        public Task<ErrorInfo> WriteServerProperty(Guid id, string key, string value);
    }
}
