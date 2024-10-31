using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Knock.Cluster.Models
{
    public class WhitelistedUsers : List<WhitelistedUser> { }

    public class WhitelistedUser
    {
        [JsonPropertyName("uuid")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
