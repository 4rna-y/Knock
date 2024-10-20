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
        private int usedMemory;

        public ProcessesManager(IConfiguration config, JavaProcessProvider java) 
        {
            this.config = config;
            this.java = java;
            processes = new ConcurrentDictionary<Guid, Process>();
        }

        public IResult Run(Guid id, LaunchInfo info, string dir)
        {
            if (processes.ContainsKey(id)) return new Error(1, "already locked");
            if (usedMemory + info.MemoryAmount > int.Parse(config["memory-amount"])) 
                return new Error(2, "reached max memory amount");

            Process process = java.GetJavaProcess(info, dir);

            usedMemory += info.MemoryAmount;

            return new Ok();
        }
    }
}
