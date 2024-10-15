using Knock.Cluster.Models;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Cluster.Services
{
    public class ContainerService
    {
        private readonly IConfiguration config;
        private readonly ILogger logger;
        private readonly HttpService http;
        private readonly JsonService json;

        private DirectoryInfo containerDir;
        private ConcurrentBag<Guid> launchedContainer;

        public ContainerService(
            IConfiguration config,
            ILogger logger,
            HttpService http,
            JsonService json)
        {
            this.config = config;
            this.logger = logger;
            this.http = http;
            this.json = json;

            containerDir = new DirectoryInfo("containers");
            launchedContainer = new ConcurrentBag<Guid>();

            if (!containerDir.Exists) containerDir.Create();
        }

        public async Task<ErrorInfo> Create(Func<ContainerBuilder, ContainerBuilder> builderPredicate)
        {
            try 
            {
                ContainerBuilder builder = builderPredicate(new ContainerBuilder());

                DirectoryInfo dir = containerDir.CreateSubdirectory(builder.Id.ToString());

                string name = "";
                if (builder.ServerApplication == 0)
                {
                    name = "paper";
                }

                string appName = $"{name}-{builder.Version}.jar";
                LaunchInfo launchInfo = new LaunchInfo(appName, builder.MemoryAmount, name, builder.Version);

                await http.Download(builder.DirectDownloadLink, dir, appName);
                await CreateServerProperties(dir);
                await CreateEulaFile(dir);
                await json.CreateFile(launchInfo, dir, "launchinfo.json");
                
                return new ErrorInfo();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return new ErrorInfo(ex);
            }
        }

        private async Task CreateServerProperties(DirectoryInfo dir)
        {
            using (StreamWriter sw = File.CreateText(Path.Combine(dir.FullName, "server.properties")))
            {
                await sw.FlushAsync();
                sw.Close();
            }
        }

        private async Task CreateEulaFile(DirectoryInfo dir)
        {
            using (StreamWriter sw = File.CreateText(Path.Combine(dir.FullName, "eula.txt")))
            {
                await sw.WriteLineAsync("eula=true");
                await sw.FlushAsync();
                sw.Close();
            }
        }

        public bool IsExists(Guid id)
        {
            return containerDir.EnumerateDirectories().Any(x => x.Name.Equals(id.ToString()));
        }

        public async Task<ErrorInfo> WriteServerProperty(Guid id, string key, string value)
        {
            try
            {
                bool found = false;
                string path = Path.Combine(containerDir.FullName, id.ToString(), "server.properties");
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] kv = lines[i].Split("=");
                    if (kv[0] == key)
                    {
                        lines[i] = $"{key}={value}";
                        found = true;
                    }
                }

                string res = string.Join("\n", lines);

                if (!found) res += $"\n{key}={value}";

                using (StreamWriter sw = new StreamWriter(path, false))
                {
                    await sw.WriteAsync(res);
                    await sw.FlushAsync();
                    sw.Close();
                }

                return new ErrorInfo();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return new ErrorInfo(ex);
            }
        }

        public Task<string> GetServerPropertyValue(Guid id, string key)
        {
            string path = Path.Combine(containerDir.FullName, id.ToString(), "server.properties");
            string[] lines = File.ReadAllLines(path);

            string line = lines.FirstOrDefault(x => x.StartsWith(key + "="));
            if (line is null) return Task.FromResult("");
            
            string[] kv = line.Split("=");
            if (kv.Length != 2) return Task.FromResult("");

            return Task.FromResult(kv[1]);
        }

        public async Task<ErrorInfo> Launch(Guid id)
        {
            if (launchedContainer.Contains(id)) return new ErrorInfo(1);

        }
    }
}
