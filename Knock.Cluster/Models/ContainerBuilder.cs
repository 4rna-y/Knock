using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Cluster.Models
{
    public class ContainerBuilder
    {
        public Guid Id { get; set; }
        public string Version { get; set; }
        public int MemoryAmount { get; set; }
        public string DirectDownloadLink { get; set; }
        public int ServerApplication {  get; set; }
        public ContainerBuilder()
        {
            
        }

        public ContainerBuilder WithId(Guid id)
        {
            Id = id;
            return this;
        }

        public ContainerBuilder WithVersion(string version)
        {
            Version = version; 
            return this;
        }

        public ContainerBuilder WithMemoryAmount(int memoryAmount)
        {
            MemoryAmount = memoryAmount;
            return this;
        }

        public ContainerBuilder WithDirectDownloadLink(string directDownloadLink)
        {
            DirectDownloadLink = directDownloadLink;
            return this;
        }

        public ContainerBuilder WithServerApplication(int serverApplication)
        {
            ServerApplication = serverApplication;
            return this;
        }
    }
}
