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
        public int ContainerCount { get; set; }
        public int MaxContainerCount { get; set; }
        public int UsingMemoryAmount { get; set; }
        public int MaxMemoryAmount { get; set; }

        public ServerStatus(byte[] data)
        {
            ContainerCount = BitConverter.ToInt32(data, sizeof(int) * 0);
            MaxContainerCount = BitConverter.ToInt32(data, sizeof(int) * 1);
            UsingMemoryAmount = BitConverter.ToInt32(data, sizeof(int) * 2);
            MaxMemoryAmount = BitConverter.ToInt32(data, sizeof(int) * 3);
        }
    }
}
