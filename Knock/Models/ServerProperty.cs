using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Knock.Models
{
    /// <summary>
    /// Json model for representing server.properties
    /// </summary>
    public class ServerProperty
    {
        [JsonPropertyName("hidden")]
        public bool Hidden { get; set; }

        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("defaultvalue")]
        public string DefaultValue { get; set; }
        
        [JsonPropertyName("range")]
        public RangedValue Range { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("options")]
        public List<ValueOption> Options { get; set; } = new List<ValueOption>();
        
        [JsonPropertyName("locale")]
        public PropertyLocale Locale { get; set; }
    }

    /// <summary>
    /// Json model for representing a selectable option with localed discription. 
    /// </summary>
    public class ValueOption
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }
        
        [JsonPropertyName("locale")]
        public PropertyLocale Locale { get; set; }
    }

    /// <summary>
    /// Json model for representing localed description.
    /// </summary>
    public class PropertyLocale
    {
        [JsonPropertyName("en")]
        public string English { get; set; }

        [JsonPropertyName("ja")]
        public string Japanese { get; set; }

        public string GetLocale(string key) => key switch
        {
            "en" => English,
            "ja" => Japanese,
            _ => English,
        };
    }

    /// <summary>
    /// Json model for representing a range of integer value.
    /// </summary>
    public class RangedValue
    {
        [JsonPropertyName("min")]
        public int Min { get; set; }

        [JsonPropertyName("max")]
        public int Max { get; set; }

        public bool InRange(int value)
        {
            return Min <= value && value <= Max;
        }
    }
}
