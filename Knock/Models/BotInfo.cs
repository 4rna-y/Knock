using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Knock.Models
{
    public class BotInfo : JsonModelBase
    {
        [JsonPropertyName("last-msg")]
        public ulong LastMsg { get; set; } = 0;

        public BotInfo() : base("info")
        {
                
        }
    }
}
