using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Models
{
    public class SelectMenuItem
    {
        public string Label { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }

        public SelectMenuItem(string label, string value, string description = null)
        {
            Label = label;
            Description = description;
            Value = value;
        }
    }
}
