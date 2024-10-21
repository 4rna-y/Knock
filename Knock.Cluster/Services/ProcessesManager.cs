using Knock.Cluster.Models;
using Knock.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Cluster.Services
{
    public class ProcessesManager
    {
        private readonly IConfiguration config;
        private readonly JavaProcessProvider java;

        private ConcurrentDictionary<Guid, Process> processes;
        private int containerCount;
        private int usedMemory;

        public ProcessesManager(IConfiguration config, JavaProcessProvider java) 
        {
            this.config = config;
            this.java = java;
            processes = new ConcurrentDictionary<Guid, Process>();
        }

        public async Task<IResult> Run(Guid id, LaunchInfo info, IContainerServerPropertiesConfigureAdapter adapter)
        {
            if (containerCount > int.Parse(config["max-container-count"])) return new Error(1, "reached max container count");
            if (processes.ContainsKey(id)) return new Error(2, "already locked");
            if (usedMemory + info.MemoryAmount > int.Parse(config["memory-amount"])) 
                return new Error(3, "reached max memory amount");

            Process process = java.GetJavaProcess(id, info);
            usedMemory += info.MemoryAmount;

            int port = int.Parse(config["container-port"]) + containerCount++;
            ErrorInfo portError = await adapter.WriteServerProperty(id, "server-port", port.ToString());
            if (!portError.Success) return new Error(4, "port error");

            processes.TryAdd(id, process);

            _ = process.RunAsync();

            return new Ok();
        }
    }
}
