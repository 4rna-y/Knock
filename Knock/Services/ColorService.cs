using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Services
{
    public class ColorService
    {
        private Dictionary<string, Color> colors = new Dictionary<string, Color>()
        {
            { "default", new Color(0x0000afb9) },
            { "success", Color.Green },
            { "warning", Color.Orange },
            { "question", Color.Purple },
            { "error", Color.Red },
            
        };
        public ColorService()
        {
            
        }
        public Color this[string key]
        {
            get => colors[key];
        }
    }
}
