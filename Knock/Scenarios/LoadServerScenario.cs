using Discord;
using Discord.WebSocket;
using Knock.Models;
using Knock.Schedules;
using System;
using System.Collections;
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
                EmbedBuilder notFoundbuilder = new EmbedBuilder()
                    .CreateLocalized("embed.load_server.owned_server_not_found")
                    .WithColor(Color["warning"]);

                ScheduleBase schedule = new UnregisterScenarioSchedule(ScenarioId, 60);
                Schedule.Resister(schedule);

                EmbedBuilder deleteEmbedBuilder = new EmbedBuilder()
                        .WithTitle(Locale.Get("embed.channel_delete.title"))
                        .WithColor(Color["default"]);

                await TextChannel.SendMessageAsync(
                    embeds: Utils.ArrayOf(notFoundbuilder.Build(), deleteEmbedBuilder.Build()));

                return;
            }



            SelectMenuBuilder selectMenuBuilder = new SelectMenuBuilder();

            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithSelectMenu();
        }
    }
}
