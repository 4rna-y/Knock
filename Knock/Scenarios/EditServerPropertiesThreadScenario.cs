using Discord;
using Discord.WebSocket;
using Knock.Models;
using Knock.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Scenarios
{
    /// <summary>
    /// 
    /// </summary>
    public class EditServerPropertiesThreadScenario : ThreadScenarioBase
    {
        private readonly Guid containerId;
        private readonly ServerPropertiesProvider provider;
        private ServerProperty serverProperty;
        private string nowPropertyValue;

        public EditServerPropertiesThreadScenario(ChannelScenarioBase channelScenario, string name, Guid containerId) : 
            base(channelScenario, name, "server-properties", channelScenario.Guild, channelScenario.User)
        {
            this.containerId = containerId;
            provider = Services.GetRequiredService<ServerPropertiesProvider>();

            Models.Add(new ScenarioModel("select-server-properties", SelectProperty));
            Models.Add(new ScenarioModel("select-property-value", SelectPropertyValue));
            Models.Add(new ScenarioModel("open-input-modal", ClickOpenInputModal));
            Models.Add(new ScenarioModel("input-value-modal", SelectPropertyValue));
            Models.Add(new ScenarioModel("close", Close));
            Models.Add(new ScenarioModel("continue", Continue));


            Models.Add(new ScenarioModel("backto-select-server-properties", async _ => await BackToSelectServerProperties()));

            Models.Add(new ScenarioModel(
                "select-server-properties-back", async arg => await SwapBackSelectMenuItem(arg, "select-server-properties")));
            Models.Add(new ScenarioModel(
                "select-server-properties-next", async arg => await SwapNextSelectMenuItem(arg, "select-server-properties")));
        }

        public override async Task Start()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder()
                .WithTitle(Locale.Get("embed.manage_server.server_properties.select_property.title"))
                .WithColor(Color["question"]);

            SelectMenuBuilder selectMenuBuilder = new SelectMenuBuilder()
                .WithCustomId($"scenario.{ScenarioId}.select-server-properties")
                .WithPlaceholder(Locale.Get("selectmenu.select_server_properties.label"));

            List<SelectMenuItem> items = new List<SelectMenuItem>();
            foreach (ServerProperty prop in provider.GetProperties())
            {
                if (prop.Hidden) continue;
                items.Add(new SelectMenuItem(prop.Key, prop.Key, prop.Locale.GetLocale(Locale.Language)));
            }

            MultiPageableSelectMenus.Add(
                "select-server-properties",
                new MultiPageableSelectMenu(items));

            foreach (SelectMenuItem item in MultiPageableSelectMenus["select-server-properties"].GetCurrentSegmentedItems())
            {
                selectMenuBuilder.AddOption(item.Label, item.Value, item.Description);
            }

            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithSelectMenu(selectMenuBuilder)
                .WithButton(
                    Locale.Get("button.server_setup.back"),
                    $"scenario.{ScenarioId}.select-server-properties-back",
                    ButtonStyle.Secondary,
                    new Emoji("◀️"),
                    disabled: true,
                    row: 1)
                .WithButton(
                    Locale.Get("button.server_setup.next"),
                    $"scenario.{ScenarioId}.select-server-properties-next",
                    ButtonStyle.Secondary,
                    new Emoji("▶️"),
                    disabled: MultiPageableSelectMenus["select-server-properties"].Items.Count < 25,
                    row: 1);

            IMessage msg = await ThreadChannel.SendMessageAsync(
                embed: embedBuilder.Build(), components: componentBuilder.Build());

            MessageIds.Add("select-server-properties", msg.Id);
            ComponentMessageIds.Add("select-server-properties", msg.Id);
        }

        private async Task BackToSelectServerProperties()
        {
            await RemoveMessage("select-property-value");
            await ToggleMessage("select-server-properties", false);
        }

        private async Task SelectProperty(SocketInteraction arg)
        {
            SocketMessageComponent component = arg as SocketMessageComponent;
            if (component is null) return;

            await ToggleMessage("select-server-properties", true);

            string key = string.Join(", ", component.Data.Values);
            ServerProperty property = provider.GetProperties().FirstOrDefault(x => x.Key.Equals(key));
            EmbedBuilder embedBuilder = new EmbedBuilder();
            ComponentBuilder componentBuilder = new ComponentBuilder();

            await component.DeferAsync();

            IDisposable typing = component.Channel.EnterTypingState();
            string nowValue = await Request.GetServerPropertyValue(containerId, property.Key);
            typing.Dispose();

            if (string.IsNullOrWhiteSpace(nowValue)) 
            {
                if (string.IsNullOrWhiteSpace(property.DefaultValue))
                    nowValue = Locale.Get("embed.manage_server.server_properties.input_value.not_set");
                else 
                    nowValue = property.DefaultValue;
            }

            if (property.Options.Count != 0)
            {
                embedBuilder
                    .WithTitle(Locale.Get("embed.manage_server.server_properties.select_value.title"))
                    .WithDescription(string.Format(
                        Locale.Get("embed.manage_server.server_properties.select_value.description"),
                        key,
                        property.Locale.GetLocale(Locale.Language),
                        nowValue))
                    .WithColor(Color["default"]);

                SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
                    .WithCustomId($"scenario.{ScenarioId}.select-property-value")
                    .WithPlaceholder(Locale.Get("selectmenu.select_property_value.label"));

                foreach (ValueOption option in property.Options)
                {
                    menuBuilder.AddOption(option.Value, option.Value, option.Locale.GetLocale(Locale.Language));
                }
                componentBuilder
                    .WithSelectMenu(menuBuilder)
                    .WithButton(
                        Locale.Get("button.server_setup.back"),
                        $"scenario.{ScenarioId}.backto-select-server-properties",
                        ButtonStyle.Secondary,
                        new Emoji("◀️"));
            }
            else
            {
                embedBuilder
                    .WithTitle(Locale.Get("embed.manage_server.server_properties.input_value.title"))
                    .WithDescription(string.Format(
                        Locale.Get("embed.manage_server.server_properties.input_value.description"),
                        key,
                        property.Locale.GetLocale(Locale.Language),
                        nowValue))
                    .WithColor(Color["question"]);

                componentBuilder
                    .WithButton(
                        Locale.Get("button.server_setup.back"),
                        $"scenario.{ScenarioId}.backto-select-server-properties",
                        ButtonStyle.Secondary,
                        new Emoji("◀️"))
                    .WithButton(
                        Locale.Get("button.server_properties.open_input_property_value"),
                        $"scenario.{ScenarioId}.open-input-modal",
                        ButtonStyle.Primary,
                        new Emoji("⌨️"));
            }

            serverProperty = property;
            nowPropertyValue = nowValue;

            IMessage msg = await component.Channel.SendMessageAsync(
                    embed: embedBuilder.Build(), components: componentBuilder.Build());
            MessageIds.Add("select-property-value", msg.Id);
            ComponentMessageIds.Add("select-property-value", msg.Id);
        }

        private async Task SelectPropertyValue(SocketInteraction arg)
        {
            string value = "";
            if (arg is SocketMessageComponent comp)
            {
                value = string.Join(", ", comp.Data.Values);
            }
            else
            if (arg is SocketModal modal)
            {
                value = modal.Data.Components.FirstOrDefault(x => x.CustomId.Equals("property-value")).Value;
            }

            await ToggleMessage("select-property-value", true);
            await arg.DeferAsync();

            bool resParse = true;
            if (serverProperty.Type == "integer")
            {
                resParse = int.TryParse(value, out int parsed);
                resParse = serverProperty.Range.InRange(parsed);
            }

            if (resParse)
            {
                IDisposable typingState = arg.Channel.EnterTypingState();

                resParse = await Request.SetServerPropertyValue(containerId, serverProperty.Key, value);

                typingState.Dispose();
            }

            if (!resParse)
            {
                EmbedBuilder errorEmbed = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.manage_server.server_properties.invalid_value.title"))
                    .WithDescription(Locale.Get("embed.manage_server.server_properties.invalid_value.description"))
                    .WithColor(Color["warning"]);

                await arg.RespondAsync(embed: errorEmbed.Build());
                return;
            }

            EmbedBuilder successEmbed = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.manage_server.server_properties.success.title"))
                    .WithDescription(
                        string.Format(
                            Locale.Get("embed.manage_server.server_properties.success.description"),
                            serverProperty.Key, value))
                    .WithColor(Color["success"]);

            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithButton(
                    Locale.Get("button.server_properties.close"),
                    $"scenario.{ScenarioId}.close",
                    ButtonStyle.Secondary,
                    new Emoji("◀"))
                .WithButton(
                    Locale.Get("button.server_properties.continue"),
                    $"scenario.{ScenarioId}.continue",
                    ButtonStyle.Primary,
                    new Emoji("▶️"));

            IMessage msg = await arg.Channel.SendMessageAsync(embed: successEmbed.Build(), components: componentBuilder.Build());
            MessageIds.Add("success", msg.Id);
            ComponentMessageIds.Add("success", msg.Id);
        }

        private async Task ClickOpenInputModal(SocketInteraction arg)
        {
            SocketMessageComponent component = arg as SocketMessageComponent;
            if (component is null) return;

            ModalBuilder modalBuilder = new ModalBuilder()
                .WithTitle(Locale.Get("modal.server_property_value.title"))
                .WithCustomId($"scenario.{ScenarioId}.input-value-modal")
                .AddTextInput(
                    Locale.Get("modal.server_property_value.label"),
                    "property-value",
                    TextInputStyle.Short,
                    placeholder: serverProperty.Range == null ?
                        Locale.Get("modal.server_property_value.placeholder") :
                        string.Format(
                            Locale.Get("modal.server_property_value.placeholder_ranged"),
                            serverProperty.Range.Min,
                            serverProperty.Range.Max));

            await arg.RespondWithModalAsync(modalBuilder.Build());
        }

        private async Task Continue(SocketInteraction arg)
        {
            await RemoveMessage("success");
            await RemoveMessage("select-property-value");
            await ToggleMessage("select-server-properties", false);
        }
    }

}
