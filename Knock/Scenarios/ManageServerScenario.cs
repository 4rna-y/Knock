using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Knock.Models;
using Knock.Services;
using Knock.Shared;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Scenarios
{
    public class ManageServerScenario : ChannelScenarioBase
    {
        private Guid serverId;
        private RestUserMessage manageMessage;

        public ManageServerScenario(Guid serverId, SocketGuild guild, SocketUser user, SocketCategoryChannel category)
            : base(guild, user, category, "manage-server")
        {
            this.serverId = serverId;

            Models.Add(new ScenarioModel("select-action", SelectManagementAction));
            Models.Add(new ScenarioModel("launch", Launch));

            Models.Add(new ScenarioModel(
                "select-server-properties-back", async arg => await SwapBackSelectMenuItem(arg, "select-server-properties")));
            Models.Add(new ScenarioModel(
                "select-server-properties-next", async arg => await SwapNextSelectMenuItem(arg, "select-server-properties")));
        }

        public override async Task Start()
        {
            ServerContainer container = Server.GetContainer(serverId);
            Server.Lock(serverId);

            EmbedBuilder embedBuilder = new EmbedBuilder()
                .WithTitle(string.Format(Locale.Get("embed.manage_server.manage_top.title"), container.Name))
                .WithColor(Color["warning"])
                .AddField(x => 
                    x.WithName(Locale.Get("embed.manage_server.manage_top.fields.app"))
                     .WithValue(container.Application.ToString())
                     .WithIsInline(true))
                .AddField(x =>
                    x.WithName(Locale.Get("embed.manage_server.manage_top.fields.version"))
                     .WithValue(container.Version)
                     .WithIsInline(true))
                .AddField(x =>
                    x.WithName(Locale.Get("embed.manage_server.manage_top.fields.memory"))
                     .WithValue(container.MemoryAmount + "GB")
                     .WithIsInline(true))
                .WithCurrentTimestamp()
                .WithAuthor(User);

            SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder(Locale.Get("selectmenu.manage_server_actions.label"))
                .WithCustomId($"scenario.{ScenarioId}.select-action")
                .AddOption(new SelectMenuOptionBuilder()
                    .WithLabel(Locale.Get("selectmenu.manage_server_actions.options.edit_server_properties.title"))
                    .WithDescription(Locale.Get("selectmenu.manage_server_actions.options.edit_server_properties.description"))
                    .WithEmote(new Emoji(Locale.Get("selectmenu.manage_server_actions.options.edit_server_properties.icon")))
                    .WithValue("server-properties"));

            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder)
                .WithButton(
                    Locale.Get("button.manage_server.launch"),
                    $"scenario.{ScenarioId}.launch",
                    ButtonStyle.Success,
                    new Emoji("🏃"),
                    row: 1);

            await Scenario.ChangeChannelName(this, Locale.Get("channel_name.manage_server") + container.Name);

            manageMessage = await TextChannel.SendMessageAsync(
                embed: embedBuilder.Build(), 
                components: componentBuilder.Build());

            MessageIds.Add("manage", manageMessage.Id);
            ComponentMessageIds.Add("manage", manageMessage.Id);
        }

        private async Task SelectManagementAction(SocketInteraction arg)
        {
            SocketMessageComponent component = arg as SocketMessageComponent;
            if (component is null) return;

            await ToggleMessage("manage", true);

            string text = string.Join(", ", component.Data.Values);

            if (text.Equals("server-properties"))
            {
                await ActionEditServerProperties(component);
            }

            await ToggleMessage("manage", false);
        }

        private async Task ActionEditServerProperties(SocketMessageComponent component)
        {
            if (Threads.ContainsKey("server-properties"))
            {
                EmbedBuilder alreadyEmbedBuilder = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.manage_server.already_created_thread.title"))
                    .WithColor(Color["error"]);

                await component.RespondAsync(embed: alreadyEmbedBuilder.Build(), ephemeral: true);
                return;
            }

            EditServerPropertiesThreadScenario scenario = await Scenario.Register(
                new EditServerPropertiesThreadScenario(this, Locale.Get("threads.edit_server_properties"), serverId));

            await component.DeferAsync();
        }

        private async Task Launch(SocketInteraction arg)
        {
            await ToggleMessage("manage", true);
            IResult res = await Request.Launch(serverId);
            await ToggleMessage("manage", false);

            await arg.RespondAsync(res.IsSuccess.ToString());
        }
    }
}
