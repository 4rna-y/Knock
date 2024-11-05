using Knock.Cluster.Models;
using Knock.Shared;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Knock.Cluster.Services
{
    public class ContainerService : IContainerServerPropertiesConfigureAdapter
    {
        private readonly IConfiguration config;
        private readonly ILogger logger;
        private readonly HttpService http;
        private readonly JsonService json;
        private readonly ProcessesManager process;

        private DirectoryInfo containerDir;
        private DirectoryInfo tmpDir;

        public ContainerService(
            IConfiguration config,
            ILogger logger,
            HttpService http,
            JsonService json,
            ProcessesManager process)
        {
            this.config = config;
            this.logger = logger;
            this.http = http;
            this.json = json;
            this.process = process;

            containerDir = new DirectoryInfo("containers");
            tmpDir = new DirectoryInfo("tmp");

            if (!containerDir.Exists) containerDir.Create();
            if (!tmpDir.Exists) tmpDir.Create();
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

        public async Task<IResult> Launch(Guid id)
        {
            LaunchInfo info =
                await json.LoadFile<LaunchInfo>(
                    new DirectoryInfo(Path.Combine(containerDir.FullName, id.ToString())),
                    "launchinfo.json");

            return await process.Run(id, info, this);
        }

        public async Task<IResult> Stop(Guid id)
        {
            return await process.Stop(id);
        }

        public async Task<string> GetLog(Guid id)
        {
            return await process.GetLog(id);
        }

        public Task<IResult> GetOpedIds(Guid id)
        {
            string path = Path.Combine(containerDir.FullName, id.ToString(), "ops.json");
            Ops ops = JsonSerializer.Deserialize<Ops>(File.ReadAllText(path));
            List<Guid> ids = ops.Select(x => x.Id).ToList();
            return Task.FromResult(new Ok(0, string.Join(",", ids)) as IResult);
        }

        public async Task<IResult> AddOpedIds(Guid id, List<Guid> uuids)
        {
            try
            {
                string path = Path.Combine(containerDir.FullName, id.ToString(), "ops.json");
                Ops ops = JsonSerializer.Deserialize<Ops>(File.ReadAllText(path));

                foreach (Guid uuid in uuids)
                {
                    if (!ops.Any(x => x.Id == uuid))
                    {
                        HttpResponseModel<JsonDocument> response =
                            await http.Get(
                                $"https://sessionserver.mojang.com/session/minecraft/profile/{uuid}");

                        if (response.Code == HttpStatusCode.OK)
                        {
                            Operator op = new Operator()
                            {
                                Id = uuid,
                                Name = response.Result.RootElement.GetProperty("name").GetString(),
                                Level = 4,
                                BypassesPlayerLimit = true
                            };
                            ops.Add(op);
                        }
                    }
                }

                string json = JsonSerializer.Serialize(ops);
                File.Delete(path);

                using (StreamWriter sw = File.CreateText(path))
                {
                    await sw.WriteLineAsync(json);
                    await sw.FlushAsync();
                    sw.Close();
                }

                return new Ok();
            }
            catch (IOException)
            {
                return new Error(1, "io error");
            }
            catch (JsonException)
            {
                return new Error(2, "format error");
            }
        }

        public async Task<IResult> RemoveOpedIds(Guid id, List<Guid> uuids)
        {
            try
            {
                string path = Path.Combine(containerDir.FullName, id.ToString(), "ops.json");
                Ops ops = JsonSerializer.Deserialize<Ops>(File.ReadAllText(path));

                foreach (Guid uuid in uuids)
                {
                    if (ops.FirstOrDefault(x => x.Id == uuid) is Operator op)
                    {
                        ops.Remove(op);
                    }
                }

                string json = JsonSerializer.Serialize(ops);
                File.Delete(path);

                using (StreamWriter sw = File.CreateText(path))
                {
                    await sw.WriteLineAsync(json);
                    await sw.FlushAsync();
                    sw.Close();
                }

                return new Ok();
            }
            catch (IOException)
            {
                return new Error(1, "io error");
            }
            catch (JsonException)
            {
                return new Error(2, "format error");
            }
        }

        public Task<IResult> GetWhitelistedIds(Guid id)
        {
            string path = Path.Combine(containerDir.FullName, id.ToString(), "whitelist.json");
            WhitelistedUsers wUsers = 
                JsonSerializer.Deserialize<WhitelistedUsers>(File.ReadAllText(path));
            List<Guid> uuids = wUsers.Select(x => x.Id).ToList();
            return Task.FromResult(new Ok(0, string.Join(",", uuids)) as IResult); 
        }

        public async Task<IResult> AddWhitelistedIds(Guid id, List<Guid> uuids)
        {
            try
            {
                string path = Path.Combine(containerDir.FullName, id.ToString(), "whitelist.json");
                WhitelistedUsers wUsers =
                    JsonSerializer.Deserialize<WhitelistedUsers>(File.ReadAllText(path));

                foreach (Guid uuid in uuids)
                {
                    if (!wUsers.Any(x => x.Id == uuid))
                    {
                        HttpResponseModel<JsonDocument> response =
                            await http.Get(
                                $"https://sessionserver.mojang.com/session/minecraft/profile/{uuid}");

                        if (response.Code == HttpStatusCode.OK)
                        {
                            WhitelistedUser op = new WhitelistedUser()
                            {
                                Id = uuid,
                                Name = response.Result.RootElement.GetProperty("name").GetString()
                            };
                            wUsers.Add(op);
                        }
                    }
                }

                string json = JsonSerializer.Serialize(wUsers);
                File.Delete(path);

                using (StreamWriter sw = File.CreateText(path))
                {
                    await sw.WriteLineAsync(json);
                    await sw.FlushAsync();
                    sw.Close();
                }

                return new Ok();
            }
            catch (IOException)
            {
                return new Error(1, "io error");
            }
            catch (JsonException)
            {
                return new Error(2, "format error");
            }
        }

        public async Task<IResult> RemoveWhitelistedIds(Guid id, List<Guid> uuids)
        {
            try
            {
                string path = Path.Combine(containerDir.FullName, id.ToString(), "whitelist.json");
                WhitelistedUsers wUsers =
                    JsonSerializer.Deserialize<WhitelistedUsers>(File.ReadAllText(path));

                foreach (Guid uuid in uuids)
                {
                    if (wUsers.FirstOrDefault(x => x.Id == uuid) is WhitelistedUser wUser)
                    {
                        wUsers.Remove(wUser);
                    }
                }

                string json = JsonSerializer.Serialize(wUsers);
                File.Delete(path);

                using (StreamWriter sw = File.CreateText(path))
                {
                    await sw.WriteLineAsync(json);
                    await sw.FlushAsync();
                    sw.Close();
                }

                return new Ok();
            }
            catch (IOException)
            {
                return new Error(1, "io error");
            }
            catch (JsonException)
            {
                return new Error(2, "format error");
            }
        }

        public async Task<IResult> AttachFile(Guid id, FileAttachment file)
        {
            try
            {
                string path = Path.Combine(containerDir.FullName, id.ToString());
                DirectoryInfo dir = new DirectoryInfo(path);

                string ext = Path.GetExtension(file.Name);
                if (ext == ".zip")
                {

                    Guid dId = Guid.NewGuid();
                    string zipPath = await http.Download(file.Url, tmpDir, dId.ToString() + ext);
                    string zipExtPath = Path.Combine(tmpDir.FullName, dId.ToString());
                    await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, zipExtPath));
                    DirectoryInfo zipExt = new DirectoryInfo(zipExtPath);

                    bool isWorldData = zipExt.Contains("level.dat");
                    bool isDataPack = zipExt.Contains("pack.mcmeta");

                    string worldName = await GetServerPropertyValue(id, "level-name");
                    if (isDataPack)
                    {
                        DirectoryInfo worldDir = dir.GetDirectory(worldName);
                        DirectoryInfo dataPacksDir = worldDir.GetDirectory("datapacks");

                        if (dataPacksDir is null) return new Error(2, "not supported");
                        File.Move(zipPath, Path.Combine(dataPacksDir.FullName, file.Name));

                        return new Ok();
                    }

                    if (isWorldData)
                    {
                        DirectoryInfo worldDir = dir.GetDirectory(worldName);
                        Directory.Move()
                    }
                }
                else 
                    return new Error(1, "invaild format");

            }
            catch (IOException)
            {
                return 
            }
        }
    }
}
