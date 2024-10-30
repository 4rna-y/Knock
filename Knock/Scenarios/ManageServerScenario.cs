using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Knock.Models;
using Knock.Services;
using Knock.Shared;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Scenarios
{
    public class ManageServerScenario : ChannelScenarioBase
    {
        private bool _isLaunched;
        private Guid serverId;
        private RestUserMessage manageMessage;

        

        public ManageServerScenario(Guid serverId, SocketGuild guild, SocketUser user, SocketCategoryChannel category)
            : base(guild, user, category, "manage-server")
        {
            this.serverId = serverId;

            Models.Add(new ScenarioModel("select-action", SelectManagementAction));
            Models.Add(new ScenarioModel("launch", Launch));
            Models.Add(new ScenarioModel("stop", Stop));

            Models.Add(new ScenarioModel(
                "select-server-properties-back", async arg => await SwapBackSelectMenuItem(arg, "select-server-properties")));
            Models.Add(new ScenarioModel(
                "select-server-properties-next", async arg => await SwapNextSelectMenuItem(arg, "select-server-properties")));
        }

        public override async Task Start()
        {
            ServerContainer container = Server.GetContainer(serverId);
            Server.Lock(serverId);

            foreach (ulong ownerId in container.Owners)
            {
                await TextChannel.AddPermissionOverwriteAsync(
                    Guild.GetUser(ownerId), OverwritePermissions.AllowAll(TextChannel));
            }

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
                    .WithValue("server-properties"))
                .AddOption(new SelectMenuOptionBuilder()
                    .WithLabel(Locale.Get("selectmenu.manage_server_actions.options.manage_owner.title"))
                    .WithDescription(Locale.Get("selectmenu.manage_server_actions.options.manage_owner.description"))
                    .WithEmote(new Emoji(Locale.Get("selectmenu.manage_server_actions.options.manage_owner.icon")))
                    .WithValue("manage-owner"));

            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder)
                .WithButton(
                    Locale.Get("button.manage_server.launch"),
                    $"scenario.{ScenarioId}.launch",
                    ButtonStyle.Success,
                    new Emoji("🏃"),
                    row: 1)
                .WithButton(
                    Locale.Get("button.manage_server.stop"),
                    $"scenario.{ScenarioId}.stop",
                    ButtonStyle.Danger,
                    new Emoji("⏹️"),
                    row: 2,
                    disabled: true);

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
            else
            if (text.Equals("manage-owner")) await ActionManageOwner(component);

            await ToggleMessage("manage", false);
            await ToggleLaunchAndStop();
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

        private async Task ActionManageOwner(SocketMessageComponent componentArg)
        {
            if (Threads.ContainsKey("manage-owner"))
            {
                EmbedBuilder alreadyEmbedBuilder = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.manage_server.already_created_thread.title"))
                    .WithColor(Color["error"]);

                await componentArg.RespondAsync(embed: alreadyEmbedBuilder.Build(), ephemeral: true);
                return;
            }

            ManageOwnerThreadScenario scenario = await Scenario.Register(
                new ManageOwnerThreadScenario(this, Locale.Get("threads.manage_owner"), serverId));

            await componentArg.DeferAsync();
        }

        private async Task Launch(SocketInteraction arg)
        {
            await arg.DeferAsync();
            await ToggleMessage("manage", true);
            IResult res = await Request.Launch(serverId);

            _isLaunched = res.IsSuccess;

            List<Embed> embeds = new List<Embed>();
            EmbedBuilder resultEmbed = new EmbedBuilder();
            if (res.IsSuccess)
            {
                resultEmbed
                    .WithTitle(Locale.Get("embed.manage_server.launch.success.title"))
                    .WithDescription(Locale.Get("embed.manage_server.launch.success.description"))
                    .WithColor(Color["success"]);

                await Scenario.Register(
                    new ServerContainerLogOutputThreadScenario(this, Locale.Get("threads.log_output"), serverId));
            }
            else
            {
                resultEmbed
                    .WithTitle(Locale.Get("embed.manage_server.launch.failed.title"))
                    .WithDescription(
                        string.Format(
                            Locale.Get("embed.manage_server.launch.failed.description"),
                            res.Code,
                            res.Message))
                    .WithColor(Color["warning"]);
            }

            embeds.Add(resultEmbed.Build());

            await ToggleMessage("manage", false);
            await ToggleLaunchAndStop();

            await arg.Channel.SendMessageAsync(embeds: embeds.ToArray());
        }

        private async Task Stop(SocketInteraction arg)
        {
            await arg.DeferAsync();
            await ToggleMessage("manage", true);
            IResult res = await Request.Stop(serverId);

            _isLaunched = !res.IsSuccess;

            List<Embed> embeds = new List<Embed>();
            EmbedBuilder resultEmbed = new EmbedBuilder();
            if (res.IsSuccess)
            {
                resultEmbed
                    .WithTitle(Locale.Get("embed.manage_server.stop.success.title"))
                    .WithColor(Color["success"]);

                await Scenario.Unregister(Threads["log-output"].ScenarioId);
                await Threads["log-output"].ThreadChannel.DeleteAsync();
                (Threads["log-output"] as ServerContainerLogOutputThreadScenario).OnDelete();
                Threads.Remove("log-output");
                
            }
            else
            {
                resultEmbed
                    .WithTitle(Locale.Get("embed.manage_server.stop.failed.title"))
                    .WithDescription(
                        string.Format(
                            Locale.Get("embed.manage_server.stop.failed.description"),
                            res.Code,
                            res.Message))
                    .WithColor(Color["warning"]);
            }

            embeds.Add(resultEmbed.Build());

            await ToggleMessage("manage", false);
            await ToggleLaunchAndStop();

            await arg.Channel.SendMessageAsync(embeds: embeds.ToArray());
        }

        public override async Task OnCloseButtonClick(SocketInteraction arg)
        {
            if (_isLaunched)
            {
                EmbedBuilder resultEmbed = new EmbedBuilder()
                    .CreateLocalized("embed.manage_server.on_close")
                    .WithColor(Color["warning"]);

                await arg.RespondAsync(embed: resultEmbed.Build(), ephemeral: true);

                return;
            }
            Server.Unlock(serverId);
            await base.OnCloseButtonClick(arg);
        }

        private async Task ToggleLaunchAndStop()
        {
            await ToggleMessagePart("manage", $"scenario.{ScenarioId}.launch", _isLaunched);
            await ToggleMessagePart("manage", $"scenario.{ScenarioId}.stop", !_isLaunched);
        }
        
    }
}
