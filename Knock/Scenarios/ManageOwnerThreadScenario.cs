using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Scenarios
{
    public class ManageOwnerThreadScenario : ThreadScenarioBase
    {
        public ManageOwnerThreadScenario(ChannelScenarioBase channelScenario, string name) : 
            base(channelScenario, name, "manage-owner", channelScenario.Guild, channelScenario.User)
        {

        }

        public override async Task Start()
        {
            EmbedBuilder selectOperationEmbed = new EmbedBuilder()
                .CreateLocalized("embed.manage_server.manage_owner.select_operation")
                .WithColor(Color["question"]);
            ComponentBuilder component = new ComponentBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId($"scenario.{ScenarioId}.select-server-properties")
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
        }
    }
}
