using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Knock.Cluster.Models
{
    public class Container
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("app-name")]
        public string ApplicationName { get; set; }
        
        [JsonPropertyName("version")]
        public string Version { get; set; }
        
        [JsonPropertyName("owners")]
        public List<ulong> Owners { get; set; } = new List<ulong>();

        [JsonPropertyName("server")]
        public string ServerAddress { get; set; }

        [JsonConstructor]
        public Container()
        {
            
        }
    }
}
