using Discord;
using Discord.WebSocket;
using Knock.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Scenarios
{
    public class ManageOwnerThreadScenario : ThreadScenarioBase
    {
        private Guid containerId;

        public ManageOwnerThreadScenario(ChannelScenarioBase channelScenario, string name, Guid containerId) : 
            base(channelScenario, name, "manage-owner", channelScenario.Guild, channelScenario.User)
        {
            this.containerId = containerId;

            Models.Add(new ScenarioModel("select-operation", SelectOperation));
        }

        public override async Task Start()
        {
            EmbedBuilder selectOperationEmbed = new EmbedBuilder()
                .CreateLocalized("embed.manage_server.manage_owner.select_operation")
                .WithColor(Color["question"]);
            ComponentBuilder component = new ComponentBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId($"scenario.{ScenarioId}.select-operation")
                    .WithPlaceholder(Locale.Get("selectmenu.manage_owner.label"))
                    .AddOption(
                        Locale.Get("selectmenu.manage_owner.options.add.title"),
                        "add",
                        Locale.Get("selectmenu.manage_owner.options.add.description"),
                        new Emoji(Locale.Get("selectmenu.manage_owner.options.add.icon")))
                    .AddOption(
                        Locale.Get("selectmenu.manage_owner.options.remove.title"),
                        "remove",
                        Locale.Get("selectmenu.manage_owner.options.remove.description"),
                        new Emoji(Locale.Get("selectmenu.manage_owner.options.remove.icon"))));

            IMessage msg = await ThreadChannel.SendMessageAsync(
                embed: selectOperationEmbed.Build(),
                components: component.Build());

            MessageIds.Add("select_operation", msg.Id);
            ComponentMessageIds.Add("select_operation", msg.Id);
        }

        private async Task SelectOperation(SocketInteraction arg)
        {
            SocketMessageComponent componentArg = arg as SocketMessageComponent;

            await ToggleMessage("select_operation", true);
            
            string key = string.Join(", ", componentArg.Data.Values);

            if (key == "add") await SelectAdd(arg);
            else
            if (key == "remove") await SelectRemove(arg);
        }

        private async Task SelectAdd(SocketInteraction arg)
        {
            ServerContainer container = Server.GetContainer(containerId);
            MinecraftAccounts accs = Data.Get<MinecraftAccounts>("mcinfo");
            List<ulong> registeredIds = accs.Accounts.Select(x => x.DiscordId).ToList();

            registeredIds.RemoveAll(container.Owners.Contains);
            if (registeredIds.Count == 0)
            {
                EmbedBuilder noOwner = new EmbedBuilder()
                    .CreateLocalized("embed.manage_server.manage_owner.select_no_owner")
                    .WithColor(Color["warning"]);

                await arg.RespondAsync(embed: noOwner.Build(), ephemeral: true);
                await ToggleMessage("select_operation", false);
                return;
            }

            EmbedBuilder embed = new EmbedBuilder()
                .CreateLocalized("embed.manage_server.manage_owner.select_add_user")
                .WithColor(Color["question"]);

            SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
                .WithCustomId($"scenario.{ScenarioId}.select-add-user")
                .WithPlaceholder(Locale.Get("selectmenu.select_user.label"))
                .WithMinValues(1)
                .WithMaxValues(10);

            foreach (ulong id in registeredIds)
            {
                SocketGuildUser user = this.Guild.GetUser(id);
                menuBuilder.AddOption(new SelectMenuOptionBuilder(user.DisplayName, id.ToString()));
            }

            ComponentBuilder component = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            await arg.RespondAsync(embed: embed.Build(), components: component.Build());
        }

        private async Task SelectRemove(SocketInteraction arg)
        {
            ServerContainer container = Server.GetContainer(containerId);

            List<ulong> registeredIds = container.Owners.ToList();
            registeredIds.Remove(arg.User.Id);
            if (registeredIds.Count == 0)
            {
                EmbedBuilder noOwner = new EmbedBuilder()
                    .CreateLocalized("embed.manage_server.manage_owner.select_no_owner")
                    .WithColor(Color["warning"]);

                await arg.RespondAsync(embed: noOwner.Build(), ephemeral: true);
                await ToggleMessage("select_operation", false);
                return;
            }

            EmbedBuilder embed = new EmbedBuilder()
                .CreateLocalized("embed.manage_server.manage_owner.select_remove_user")
                .WithColor(Color["question"]);

            SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
                .WithCustomId($"scenario.{ScenarioId}.select-remove-user")
                .WithPlaceholder(Locale.Get("selectmenu.select_user.label"))
                .WithMinValues(1)
                .WithMaxValues(10);

            foreach (ulong id in registeredIds)
            {
                SocketGuildUser user = this.Guild.GetUser(id);
                menuBuilder.AddOption(new SelectMenuOptionBuilder(user.DisplayName, id.ToString()));
            }

            ComponentBuilder component = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            await arg.RespondAsync(embed: embed.Build(), components: component.Build());
        }
    }
}
