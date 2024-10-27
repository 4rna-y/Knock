using Knock.Cluster.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Cluster.Services
{
    public class JavaProcessProvider
    {
        private readonly ILogger logger;
        private readonly HttpService http;

        //0: java version
        //1: os
        //2: arch
        private readonly string jrePath = 
            "https://api.adoptium.net/v3/binary/latest/{0}/ga/{1}/{2}/jdk/hotspot/normal/eclipse";
        private readonly int[] jreVersions = new int[]
        {
            11, 16, 17, 21
        };

        private DirectoryInfo dir;

        public JavaProcessProvider(
            ILogger logger,
            HttpService http)
        {
            this.logger = logger;
            this.http = http;
        }

        public async Task Setup()
        {
            string os = "";
            Architecture arch = Architecture.X64;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) os = "windows";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) os = "linux";
            arch = RuntimeInformation.OSArchitecture;
            
            bool[] installed = new bool[4];

            dir = new DirectoryInfo("java");
            if (!dir.Exists) dir.Create();
            DirectoryInfo[] verDirs = dir.GetDirectories();
            for (int i = 0; i < verDirs.Length; i++)
            {
                if (verDirs[i].Name == "11") installed[0] = true;
                if (verDirs[i].Name == "16") installed[1] = true;
                if (verDirs[i].Name == "17") installed[2] = true;
                if (verDirs[i].Name == "21") installed[3] = true;
            }

            for (int i = 0; i < jreVersions.Length; i++)
            {
                if (!installed[i])
                {
                    logger.Info($"Downloading jdk {jreVersions[i]}...");
                    DirectoryInfo target = dir.CreateSubdirectory(jreVersions[i].ToString());
                    string reqPath = string.Format(jrePath, jreVersions[i], os, arch.ToString().ToLower());
                    string zipPath = await http.Download(reqPath, target, $"{jreVersions[i]}.zip");
                    logger.Info($"Extracting jre {jreVersions[i]}...");
                    ZipFile.ExtractToDirectory(zipPath, target.FullName);
                    File.Delete(zipPath);
                }
            }
        }

        public Process GetJavaProcess(Guid id, LaunchInfo info)
        {
            string java = GetJavaPath(info.Version);
            DirectoryInfo dir = new DirectoryInfo(Path.Combine("containers", id.ToString()));
            Process process = new Process();
            ProcessStartInfo processInfo = new ProcessStartInfo()
            {
                FileName = java,
                Arguments = $"-server -Xms{info.MemoryAmount}G -Xmx{info.MemoryAmount}G -jar {info.AppName}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                WorkingDirectory = dir.FullName,
            };
            process.EnableRaisingEvents = true;
            process.StartInfo = processInfo;
            return process;
        }

        private int GetMinorVersion(string version)
        {
            string[] vs = version.Split('.');
            return int.Parse(vs[1]);
        }

        private int GetPatchVersion(string version)
        {
            string[] vs = version.Split('.');
            if (vs.Length == 3) return int.Parse(vs[2]);
            return 0;
        }

        private string GetJavaPath(string v)
        {
            int minor = GetMinorVersion(v);
            int patch = GetPatchVersion(v);
            DirectoryInfo vDir = null;

            if (minor <= 16)                    vDir = dir.GetDirectories().FirstOrDefault(x => x.Name == "11");
            else if (minor == 17 && patch <= 1) vDir = dir.GetDirectories().FirstOrDefault(x => x.Name == "16");
            else if (minor >= 18 && minor <= 20)
            {
                if (minor == 20 && patch >= 5)  vDir = dir.GetDirectories().FirstOrDefault(x => x.Name == "21");
                else                            vDir = dir.GetDirectories().FirstOrDefault(x => x.Name == "17");
            }
            else if (minor >= 20)               vDir = dir.GetDirectories().FirstOrDefault(x => x.Name == "21");

            string ext = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ext = ".exe";
            }

            FileInfo bin = 
                vDir.GetDirectories()[0]
                    .GetDirectories().FirstOrDefault(x => x.Name == "bin")
                    .GetFiles().FirstOrDefault(x => x.Name.StartsWith($"java{ext}"));
            return bin.FullName;
        }
    }
}
