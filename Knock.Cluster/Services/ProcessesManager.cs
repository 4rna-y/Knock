using Knock.Cluster.Models;
using Knock.Shared;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Knock.Cluster.Services
{
    public class ProcessesManager
    {
        private readonly ILogger logger;
        private readonly IConfiguration config;
        private readonly JavaProcessProvider java;

        private ConcurrentDictionary<Guid, Process> processes;
        private ConcurrentDictionary<Guid, ConcurrentBag<string>> logs;
        private ConcurrentDictionary<int, bool> ports;
        private int containerCount;
        private int usedMemory;
        private int initPort;
        private int maxPort;

        public ProcessesManager(ILogger logger, IConfiguration config, JavaProcessProvider java) 
        {
            this.logger = logger;
            this.config = config;
            this.java = java;
            processes = new ConcurrentDictionary<Guid, Process>();
            logs = new ConcurrentDictionary<Guid, ConcurrentBag<string>>();
            ports = new ConcurrentDictionary<int, bool>();

            initPort = int.Parse(config["container-port"]);
            maxPort = int.Parse(config["max-container-count"]);

            for (int i = 0; i < maxPort; i++)
            {
                ports.TryAdd(initPort + i, false);
                
            }
        }

        public int GetContainerCount() => containerCount;
        public int GetUsedMemory() => usedMemory;

        public async Task<IResult> Run(Guid id, LaunchInfo info, IContainerServerPropertiesConfigureAdapter adapter)
        {
            int port = GetPort();
            int initPort = int.Parse(config["container-port"]);
            try
            {
                
                if (port == -1) return new Error(1, "reached max container count");
                if (containerCount > int.Parse(config["max-container-count"])) return new Error(1, "reached max container count");
                if (processes.ContainsKey(id)) return new Error(2, "already locked");
                if (usedMemory + info.MemoryAmount > int.Parse(config["memory-amount"]))
                    return new Error(3, "reached max memory amount");

                Process process = java.GetJavaProcess(id, info);
                usedMemory += info.MemoryAmount;
                containerCount++;

                ErrorInfo portError = await adapter.WriteServerProperty(id, "server-port", port.ToString());
                if (!portError.Success) return new Error(4, "port error");

                processes.TryAdd(id, process);
                logs.TryAdd(id, new ConcurrentBag<string>());

                void e(object sender, DataReceivedEventArgs e) => logs[id].Add(e.Data);

                process.OutputDataReceived += e;
                process.ErrorDataReceived += e;

                process.Exited += (_, _) =>
                {
                    logger.Info("Container has exited");
                    logs.Remove(id, out _);
                    containerCount--;

                    processes[id].Close();
                    processes[id].Dispose();
                    processes.Remove(id, out _);
                    ports[port] = false;
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                logger.Info("Container has been started.");
            }
            catch (Exception e)
            {
                logger.Error(e); 
            }

            return new Ok(0, (port - initPort).ToString());
        }

        public async Task<IResult> Stop(Guid id)
        {
            if (!processes.ContainsKey(id)) return new Ok();
            if (processes[id].HasExited)
            {
                return new Ok();
            }

            using (StreamWriter sw = processes[id].StandardInput)
            {
                await sw.WriteLineAsync("stop");
            }
            
            if (!await processes[id].WaitForExitAsync(7 * 1000)) return new Error(1, "timeout");

            return new Ok();
        }

        public Task<string> GetLog(Guid id)
        {
            List<string> log = logs[id].ToList();
            logs[id].Clear();

            log.Reverse();
            return Task.FromResult(string.Join("\n", log));
        }

        private int GetPort()
        {
            for (int i = 0; i < ports.Count; i++)
            {
                ports.TryGetValue(initPort + i, out bool v);
                if (!v)
                {
                    ports[initPort + i] = true;
                    return initPort + i;
                }
            }

            return -1;
        }
    }
}
