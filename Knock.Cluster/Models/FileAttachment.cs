using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Cluster.Models
{
    public class FileAttachment
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public FileAttachment(string name, string url)
        {
            Name = name;
            Url = url;
        }
    }
}
