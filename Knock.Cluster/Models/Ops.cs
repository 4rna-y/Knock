using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Knock.Cluster.Models
{

    public class Ops : List<Operator> { }

    public class Operator
    {
        [JsonPropertyName("uuid")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("bypassesPlayerLimit")]
        public bool BypassesPlayerLimit { get; set; }
    }
}
