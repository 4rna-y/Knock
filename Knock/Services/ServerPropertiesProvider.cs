using Knock.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Knock.Services
{
    public class ServerPropertiesProvider
    {
        private List<ServerProperty> serverProperties;
        public ServerPropertiesProvider() 
        {
            serverProperties = JsonSerializer.Deserialize<List<ServerProperty>>(File.ReadAllText("server_properties.json"));
        }

        public List<ServerProperty> GetProperties()
        {
            return serverProperties;
        }
    }
}
