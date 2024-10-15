using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Knock.Services
{
    public class LocaleService
    {
        public string Language { get; set; } = "en";

        public LocaleService()
        {
            
        }
        
        public string Get(params string[] keys)
        {
            StringBuilder sb = new StringBuilder();
            string path = Path.Combine("locale", Language + ".json");
            if (!File.Exists(path)) return "[locale error]";
            JsonDocument jd = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement elem = jd.RootElement;
            for (int y = 0; y < keys.Length; y++)
            {
                string[] seg = keys[y].Split(".");
                int x = 0;
                for (; x < seg.Length; x++)
                {
                    if (!elem.TryGetProperty(seg[x], out elem)) break; 
                }
                if (x == seg.Length)
                {
                    sb.Append(elem.GetString());
                }
                elem = jd.RootElement;
            }

            return sb.ToString();
        }
    }
}
