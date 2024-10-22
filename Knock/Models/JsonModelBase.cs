using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Knock.Models
{
    /// <summary>
    /// Base model for representing json model.
    /// </summary>
    public class JsonModelBase
    {
        [JsonIgnore]
        public string Name { get; }
        protected JsonModelBase(string name)
        {
            Name = name;
        }
    }
}
