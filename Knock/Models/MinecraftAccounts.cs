using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Knock.Models
{
    /// <summary>
    /// Json model for representing minecraft account list.
    /// </summary>
    public class MinecraftAccounts : JsonModelBase
    {
        [JsonPropertyName("accounts")]
        public List<MinecraftAccount> Accounts { get; set; } = new List<MinecraftAccount>();

        public MinecraftAccounts() : base("mcinfo")
        {
            
        }
    }

    /// <summary>
    /// Json model for storing minecraft account.
    /// </summary>
    public class MinecraftAccount
    {
        [JsonPropertyName("discord-id")]
        public ulong DiscordId { get; set; }

        [JsonPropertyName("minecraft-id")]
        public string MinecraftId { get; set; }

        [JsonPropertyName("minecraft-name")]
        public string MinecraftName { get; set; }

        [JsonPropertyName("is-confirmed")]
        public bool IsConfirmed { get; set; }

        public MinecraftAccount() 
        {

        }

        public MinecraftAccount(ulong did, string mid, string name, bool isComfirmed)
        {
            DiscordId = did;
            MinecraftId = mid;
            MinecraftName = name;
            IsConfirmed = isComfirmed;
        }
    }
}
