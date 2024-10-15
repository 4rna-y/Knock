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
            Models.Add(new ScenarioModel("servers", SelectContainer));

            Models.Add(new ScenarioModel(
                "servers-back", async arg => await SwapBackSelectMenuItem(arg, "servers")));
            Models.Add(new ScenarioModel(
                "servers-next", async arg => await SwapNextSelectMenuItem(arg, "servers")));
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

            EmbedBuilder embedBuilder = new EmbedBuilder()
                .CreateLocalized("embed.load_server.select_container")
                .WithColor(Color["question"]);

            List<SelectMenuItem> items = new List<SelectMenuItem>();

            foreach (ServerContainer container in containers)
            {
                items.Add(new SelectMenuItem(
                    container.Name, container.Id.ToString(),
                    string.Format(Locale.Get("selectmenu.select_container.description"), container.Version, container.LastAccessDate.ToString())));
            }

            MultiPageableSelectMenus.Add("servers", new MultiPageableSelectMenu(items));

            SelectMenuBuilder selectMenuBuilder = new SelectMenuBuilder()
                .WithCustomId($"scenario.{ScenarioId}.servers")
                .WithPlaceholder(Locale.Get("selectmenu.select_container.label"));

            foreach (SelectMenuItem item in MultiPageableSelectMenus["servers"].GetCurrentSegmentedItems())
            {
                selectMenuBuilder.AddOption(
                    new SelectMenuOptionBuilder()
                        .WithLabel(item.Label)
                        .WithValue(item.Value)
                        .WithDescription(item.Description));
            }

            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithSelectMenu(selectMenuBuilder)
                .WithButton(
                    Locale.Get("button.server_setup.back"),
                    $"scenario.{ScenarioId}.servers-back",
                    ButtonStyle.Secondary,
                    new Emoji("◀️"),
                    disabled: true,
                    row: 1)
                .WithButton(
                    Locale.Get("button.server_setup.next"),
                    $"scenario.{ScenarioId}.servers-next",
                    ButtonStyle.Secondary,
                    new Emoji("▶️"),
                    disabled: MultiPageableSelectMenus["servers"].Items.Count < 25,
                    row: 1);

            IMessage msg = await TextChannel.SendMessageAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
            
            MessageIds.Add("servers", msg.Id);
            ComponentMessageIds.Add("servers", msg.Id);
        }

        private async Task SelectContainer(SocketInteraction arg)
        {
            SocketMessageComponent component = arg as SocketMessageComponent;
            if (component is null) return;

            await ToggleMessage("servers", true);
            string key = string.Join(", ", component.Data.Values);

            Guid containerId = Guid.Parse(key);
            if (Server.IsLocked(containerId))
            {
                EmbedBuilder embedBuilder = new EmbedBuilder()
                    .CreateLocalized("embed.load_server.server_locked")
                    .WithColor(Color["warning"]);

                await arg.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                await ToggleMessage("servers", false);

                return;
            }

            await arg.DeferAsync();

            ChannelScenarioBase scenario = new ManageServerScenario(containerId, Guild, User, Category);
            await Scenario.Register(scenario);
            ScheduleBase schedule = new UnregisterScenarioSchedule(ScenarioId, 10);
            Schedule.Resister(schedule);

            IMessage msg = await scenario.TextChannel.GetMessageAsync(scenario.MentionMessageId);
            EmbedBuilder deleteEmbedBuilder = new EmbedBuilder()
                .WithTitle(Locale.Get("embed.channel_delete.title"))
                .WithDescription(
                    string.Format(Locale.Get("embed.channel_delete.description"),
                    msg.GetJumpUrl()));

            await arg.Channel.SendMessageAsync(embed: deleteEmbedBuilder.Build());
        }
    }
}
