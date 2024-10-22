using Knock.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Knock.Models
{
    /// <summary>
    /// Json model for representing container list.
    /// </summary>
    public class ServerContainers : JsonModelBase
    {
        [JsonPropertyName("containers")]
        public List<ServerContainer> Containers { get; set; } = new List<ServerContainer>();
        public ServerContainers() : base("containers")
        {
            
        }
    }

    /// <summary>
    /// Json model for storing container info
    /// </summary>
    public class ServerContainer
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("server-app")]
        public ServerApplication Application { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("app-fullname")]
        public string ApplicationFullName { get; set; }

        [JsonPropertyName("dl-link")]
        public string DownloadLink { get; set; }

        [JsonPropertyName("memory-amount")]
        public string MemoryAmount { get; set; }

        [JsonPropertyName("last-access")]
        public DateTime LastAccessDate { get; set; }

        [JsonPropertyName("stored-location")]
        public string StoredLocation { get; set; }

        [JsonPropertyName("owners")]
        public List<ulong> Owners { get; set; } = new List<ulong>();

        [JsonConstructor]
        public ServerContainer()
        {
            
        }

        public ServerContainer(
            Guid id,
            string name, 
            ServerApplication serverApplication, 
            string version, 
            string applicationFullName,
            string downloadLink,
            string memoryAmount,
            DateTime lastAccessDate,
            string storedLocation,
            List<ulong> owners)
        {
            Id = id; 
            Name = name; 
            Application = serverApplication;
            Version = version;
            ApplicationFullName = applicationFullName;
            DownloadLink = downloadLink;
            MemoryAmount = memoryAmount;
            LastAccessDate = lastAccessDate;
            StoredLocation = storedLocation;
            Owners = owners;
        }
    }
}
