using Knock.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Knock.Models
{
    public class ServerContainerBuilder
    {
        public string Name { get; set; } = null;
        public ServerApplication Application { get; set; }
        public string Version { get; set; } = null;
        public string ApplicationFullName { get; set; } = null;
        public string DownloadLink { get; set; } = null;
        public string MemoryAmount { get; set; } = null;
        public DateTime LastAccessDate { get; set; }
        public string StoredLocation { get; set; }
        public List<ulong> Owners { get; set; } = new List<ulong>();

        public ServerContainerBuilder() { }

        public ServerContainerBuilder WithName(string name) 
        {
            Name = name;
            return this;
        }

        public ServerContainerBuilder WithServerApplication(ServerApplication app)
        {
            Application = app;
            return this;
        }

        public ServerContainerBuilder WithVersion(string version)
        {
            Version = version;
            return this;
        }

        public ServerContainerBuilder WithApplicationFullName(string applicationFullName)
        {
            ApplicationFullName = applicationFullName;
            return this;
        }

        public ServerContainerBuilder WithDownloadLink(string downloadLink)
        {
            DownloadLink = downloadLink;
            return this;
        }

        public ServerContainerBuilder WithMemoryAmount(string memoryAmount)
        {
            MemoryAmount = memoryAmount;
            return this;
        }

        public ServerContainerBuilder WithLastAccessDate(DateTime lastAccessDate)
        {
            LastAccessDate = lastAccessDate;
            return this;
        }

        public ServerContainerBuilder WithStoredLocation(string storedLocation)
        {
            StoredLocation = storedLocation;
            return this;
        }

        public ServerContainerBuilder WithOwners(List<ulong> owners)
        {
            Owners = owners;
            return this;
        }

        public ServerContainer Build()
        {
            return new ServerContainer(
                Guid.NewGuid(), 
                Name, 
                Application, 
                Version, 
                ApplicationFullName, 
                DownloadLink, 
                MemoryAmount, 
                LastAccessDate, 
                StoredLocation, 
                Owners);
        }
    }
}
