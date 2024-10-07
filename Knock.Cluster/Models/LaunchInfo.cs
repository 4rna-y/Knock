using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Knock.Cluster.Models
{
    public class LaunchInfo
    {
        [JsonPropertyName("app-name")]
        public string AppName { get; set; }
        
        [JsonPropertyName("memory-amount")]
        public int MemoryAmount { get; set; }

        [JsonPropertyName("app-type")]
        public string AppType { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        public LaunchInfo(string appName, int memoryAmount, string appType, string version)
        {
            AppName = appName;
            MemoryAmount = memoryAmount;
            AppType = appType;
            Version = version;
        }

        [JsonConstructor]
        public LaunchInfo()
        {
            
        }
    }
}
