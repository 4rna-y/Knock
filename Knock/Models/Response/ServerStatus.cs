using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Models.Response
{
    /// <summary>
    /// Model for storing server status.
    /// </summary>
    public class ServerStatus
    {
        public byte ContainerCount { get; set; }
        public byte MaxContainerCount { get; set; }
        public byte UsingMemoryAmount { get; set; }
        public byte MaxMemoryAmount { get; set; }

        public ServerStatus(byte[] data)
        {
            ContainerCount = data[0];
            MaxContainerCount = data[1];
            UsingMemoryAmount = data[2];
            MaxMemoryAmount = data[3];
        }
    }
}
