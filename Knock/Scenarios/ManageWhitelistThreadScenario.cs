using Discord;
using Discord.WebSocket;
using Knock.Models;
using Knock.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Scenarios
{
    public class ManageWhitelistThreadScenario : ThreadScenarioBase
    {
        private Guid containerId;

        public ManageWhitelistThreadScenario(ChannelScenarioBase channelScenario, string name, Guid containerId) : 
            base(channelScenario, name, "manage-whitelist", channelScenario.Guild, channelScenario.User)
        {
            this.containerId = containerId;

            Models.Add(new ScenarioModel("select-operation", SelectOperation));
            Models.Add(new ScenarioModel("select-add-user", SelectAddUser));
            Models.Add(new ScenarioModel("select-remove-user", SelectRemoveUser));
            Models.Add(new ScenarioModel("close", Close));
        }

        public override async Task Start()
        {
            EmbedBuilder selectOperationEmbed = new EmbedBuilder()
                .CreateLocalized("embed.manage_server.manage_whitelist.select_operation")
                .WithColor(Color["question"]);
            ComponentBuilder component = new ComponentBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId($"scenario.{ScenarioId}.select-operation")
                    .WithPlaceholder(Locale.Get("selectmenu.manage_whitelist.label"))
                    .AddOption(
                        Locale.Get("selectmenu.manage_whitelist.options.add.title"),
                        "add",
                        Locale.Get("selectmenu.manage_whitelist.options.add.description"),
                        new Emoji(Locale.Get("selectmenu.manage_whitelist.options.add.icon")))
                    .AddOption(
                        Locale.Get("selectmenu.manage_whitelist.options.remove.title"),
                        "remove",
                        Locale.Get("selectmenu.manage_whitelist.options.remove.description"),
                        new Emoji(Locale.Get("selectmenu.manage_whitelist.options.remove.icon"))));

            IMessage msg = await ThreadChannel.SendMessageAsync(
                embed: selectOperationEmbed.Build(),
                components: component.Build());

            MessageIds.Add("select_operation", msg.Id);
            ComponentMessageIds.Add("select_operation", msg.Id);
        }

        private async Task SelectOperation(SocketInteraction arg)
        {
            SocketMessageComponent componentArg = arg as SocketMessageComponent;

            await arg.DeferAsync();
            await ToggleMessage("select_operation", true);
            
            string key = string.Join(", ", componentArg.Data.Values);

            if (key == "add") await SelectAdd(arg);
            else
            if (key == "remove") await SelectRemove(arg);
        }

        private async Task SelectAdd(SocketInteraction arg)
        {
            List<Guid> uuids = await Request.GetWhitelistedIds(containerId);
            MinecraftAccounts accs = Data.Get<MinecraftAccounts>("mcinfo");
            List<MinecraftAccount> mAccs = accs.Accounts.ToList();
            mAccs.RemoveAll(x => uuids.Contains(Guid.Parse(x.MinecraftId)));

            if (mAccs.Count == 0)
            {
                EmbedBuilder noOwner = new EmbedBuilder()
                    .CreateLocalized("embed.manage_server.manage_whitelist.select_no_user")
                    .WithColor(Color["warning"]);

                await arg.RespondAsync(embed: noOwner.Build(), ephemeral: true);
                await ToggleMessage("select_operation", false);
                return;
            }

            EmbedBuilder embed = new EmbedBuilder()
                .CreateLocalized("embed.manage_server.manage_whitelist.select_add_user")
                .WithColor(Color["question"]);

            SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
                .WithCustomId($"scenario.{ScenarioId}.select-add-user")
                .WithPlaceholder(Locale.Get("selectmenu.select_user.label"))
                .WithMinValues(1)
                .WithMaxValues(mAccs.Count);

            foreach (MinecraftAccount mAcc in mAccs)
            {
                SocketGuildUser user = this.Guild.GetUser(mAcc.DiscordId);
                menuBuilder.AddOption(new SelectMenuOptionBuilder(user.DisplayName, mAcc.MinecraftId.ToString()));
            }

            ComponentBuilder component = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            IMessage msg = await arg.Channel.SendMessageAsync(embed: embed.Build(), components: component.Build());
            MessageIds.Add("select-user", msg.Id);
            ComponentMessageIds.Add("select-user", msg.Id);
        }

        private async Task SelectRemove(SocketInteraction arg)
        {
            List<Guid> uuids = await Request.GetWhitelistedIds(containerId);
            MinecraftAccounts accs = Data.Get<MinecraftAccounts>("mcinfo");
            List<MinecraftAccount> mAccs = accs.Accounts.FindAll(x => uuids.Contains(Guid.Parse(x.MinecraftId)));

            if (mAccs.Count == 0)
            {
                EmbedBuilder noOwner = new EmbedBuilder()
                    .CreateLocalized("embed.manage_server.manage_whitelist.select_no_user")
                    .WithColor(Color["warning"]);

                await arg.RespondAsync(embed: noOwner.Build(), ephemeral: true);
                await ToggleMessage("select_operation", false);
                return;
            }

            EmbedBuilder embed = new EmbedBuilder()
                .CreateLocalized("embed.manage_server.manage_whitelist.select_remove_user")
                .WithColor(Color["question"]);

            SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
                .WithCustomId($"scenario.{ScenarioId}.select-remove-user")
                .WithPlaceholder(Locale.Get("selectmenu.select_user.label"))
                .WithMinValues(1)
                .WithMaxValues(mAccs.Count);

            foreach (MinecraftAccount mAcc in mAccs)
            {
                SocketGuildUser user = this.Guild.GetUser(mAcc.DiscordId);
                menuBuilder.AddOption(new SelectMenuOptionBuilder(user.DisplayName, mAcc.MinecraftId.ToString()));
            }

            ComponentBuilder component = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            IMessage msg = await arg.Channel.SendMessageAsync(embed: embed.Build(), components: component.Build());
            MessageIds.Add("select-user", msg.Id);
            ComponentMessageIds.Add("select-user", msg.Id);
        }

        private Task SelectAddUser(SocketInteraction arg) => SelectUser(arg, "add");

        private Task SelectRemoveUser(SocketInteraction arg) => SelectUser(arg, "remove");

        private async Task SelectUser(SocketInteraction arg, string operation)
        {
            SocketMessageComponent componentArg = arg as SocketMessageComponent;

            await arg.DeferAsync();
            await ToggleMessage("select-user", true);

            List<string> users = new List<string>(componentArg.Data.Values);
            MinecraftAccounts accs = Data.Get<MinecraftAccounts>("mcinfo");

            IResult res = null;

            if (operation == "add")
            {
                List<Guid> uuids = users.Select(Guid.Parse).ToList();
                res = await Request.AddWhitelistedIds(containerId, uuids);
            }

            if (operation == "remove")
            {
                List<Guid> uuids = users.Select(Guid.Parse).ToList();
                res = await Request.RemoveWhitelistedIds(containerId, uuids);
            }

            EmbedBuilder resultEmbed = new EmbedBuilder()
                .CreateLocalized("embed.manage_server.manage_whitelist.operation_done")
                .WithColor(Color["success"]);

            ComponentBuilder component = new ComponentBuilder()
                .WithButton(
                    Locale.Get("button.close_scenario"),
                    $"scenario.{ScenarioId}.close");

            await arg.Channel.SendMessageAsync(embed: resultEmbed.Build(), components: component.Build());
        }


    }
}
