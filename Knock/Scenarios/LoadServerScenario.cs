using Discord;
using Discord.WebSocket;
using Knock.Models;
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
            IEnumerable<ServerContainer> containers = 
                Server.GetContainers(User.Id, WebSocket.GetConnectionAdresses());

            if (!containers.Any())
            {
                EmbedBuilder builder = new EmbedBuilder()
                    .CreateLocalized("embed.load_server.owned_server_not_found")
                    .WithColor(Color["warning"]);
                Schedule.Resister()
            }
        }
    }
}
