using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Scenarios
{
    public class LoadServerScenario : ChannelScenarioBase
    {
        public LoadServerScenario(SocketGuild guild, SocketUser user, SocketCategoryChannel category) : 
            base(guild, user, category, "load-server")
        {

        }

        public override async Task Start()
        {
            
        }
    }
}
