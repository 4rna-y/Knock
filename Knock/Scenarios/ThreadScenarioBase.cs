using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Knock.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Scenarios
{
    public class ThreadScenarioBase : ScenarioBase
    {
        public RestThreadChannel ThreadChannel { get; private set; }
        public ChannelScenarioBase ChannelScenario { get; }
        public string Name { get; }
        public string Key { get; }
        protected Dictionary<string, ulong> MessageIds { get; }
        protected Dictionary<string, ulong> ComponentMessageIds { get; }
        protected Dictionary<string, MultiPageableSelectMenu> MultiPageableSelectMenus { get; }

        public ThreadScenarioBase(ChannelScenarioBase channelScenario, string name, string key, SocketGuild guild, SocketUser user) : 
            base(guild, user) 
        {
            MessageIds = new Dictionary<string, ulong>();
            ComponentMessageIds = new Dictionary<string, ulong>();
            MultiPageableSelectMenus = new Dictionary<string, MultiPageableSelectMenu>();

            ChannelScenario = channelScenario;
            Name = name;
            Key = key;
        }

        protected async Task Close(SocketInteraction arg)
        {
            await arg.DeferAsync();
            ChannelScenario.Threads.Remove(Key);
            await Scenario.Unregister(ScenarioId);
            await ThreadChannel.DeleteAsync();
        }

        protected async Task RemoveMessage(string key)
        {
            if (MessageIds.ContainsKey(key))
            {
                await ThreadChannel.DeleteMessageAsync(MessageIds[key]);
                MessageIds.Remove(key);
            }
            if (ComponentMessageIds.ContainsKey(key))
            {
                ComponentMessageIds.Remove(key);
            }
        }

        protected async Task ToggleMessage(string key, bool toggle)
        {
            if (ComponentMessageIds.ContainsKey(key))
            {
                RestMessage msg = await ThreadChannel.GetMessageAsync(ComponentMessageIds[key]);
                List<ActionRowComponent> rows = msg.Components.ToList();
                ComponentBuilder builder = new ComponentBuilder();

                foreach (ActionRowComponent row in rows)
                {
                    ActionRowBuilder rowBuilder = new ActionRowBuilder();
                    List<IMessageComponent> comps = row.Components.ToList();
                    foreach (IMessageComponent comp in comps)
                    {
                        if (comp is ButtonComponent button)
                        {
                            rowBuilder.WithButton(
                                button.Label,
                                button.CustomId,
                                button.Style,
                                button.Emote,
                                button.Url,
                                toggle);
                        }
                        else
                        if (comp is SelectMenuComponent menu)
                        {
                            List<SelectMenuOptionBuilder> optionBuilders = new List<SelectMenuOptionBuilder>();
                            foreach (SelectMenuOption option in menu.Options)
                            {
                                SelectMenuOptionBuilder optionBuilder = new SelectMenuOptionBuilder()
                                    .WithLabel(option.Label)
                                    .WithValue(option.Value)
                                    .WithDescription(option.Description)
                                    .WithEmote(option.Emote)
                                    .WithDefault(option.IsDefault.GetValueOrDefault());
                                optionBuilders.Add(optionBuilder);
                            }
                            rowBuilder.WithSelectMenu(
                                menu.CustomId,
                                optionBuilders,
                                menu.Placeholder,
                                menu.MinValues,
                                menu.MaxValues,
                                toggle,
                                menu.Type,
                                menu.ChannelTypes.ToArray());
                        }
                    }
                    builder.AddRow(rowBuilder);
                }

                await ThreadChannel.ModifyMessageAsync(ComponentMessageIds[key], x =>
                {
                    x.Components = builder.Build();
                });
            }
        }

        private async Task SwapSelectMenuItem(SocketInteraction arg, string key, Func<MultiPageableSelectMenu, List<SelectMenuItem>> fn)
        {
            RestMessage msg = await ThreadChannel.GetMessageAsync(ComponentMessageIds[key]);

            MultiPageableSelectMenu menuInfo = MultiPageableSelectMenus[key];
            List<SelectMenuItem> items = fn(menuInfo);

            ComponentBuilder builder = new ComponentBuilder();

            foreach (ActionRowComponent row in msg.Components)
            {
                ActionRowBuilder rowBuilder = new ActionRowBuilder();
                foreach (IMessageComponent component in row.Components)
                {
                    if (component is ButtonComponent b)
                    {
                        ButtonBuilder buttonBuilder = new ButtonBuilder()
                            .WithLabel(b.Label)
                            .WithCustomId(b.CustomId)
                            .WithEmote(b.Emote)
                            .WithStyle(b.Style)
                            .WithSkuId(b.SkuId)
                            .WithUrl(b.Url);

                        if (b.CustomId.Equals($"scenario.{ScenarioId}.{key}-back"))
                        {
                            if (menuInfo.Page == 0)
                                buttonBuilder.WithDisabled(true);
                            else
                                buttonBuilder.WithDisabled(false);
                        }

                        if (b.CustomId.Equals($"scenario.{ScenarioId}.{key}-next"))
                        {
                            if (Math.Ceiling(menuInfo.Items.Count / 25d) == menuInfo.Page + 1)
                                buttonBuilder.WithDisabled(true);
                            else
                                buttonBuilder.WithDisabled(false);
                        }

                        rowBuilder.WithButton(buttonBuilder);
                    }
                    if (component is SelectMenuComponent m)
                    {
                        SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
                            .WithCustomId(m.CustomId)
                            .WithPlaceholder(m.Placeholder)
                            .WithChannelTypes(m.ChannelTypes.ToList())
                            .WithType(m.Type);

                        foreach (SelectMenuItem item in items)
                        {
                            menuBuilder.AddOption(item.Label, item.Value, item.Description);
                        }

                        rowBuilder.WithSelectMenu(menuBuilder);
                    }
                }
                builder.AddRow(rowBuilder);
            }

            await ThreadChannel.ModifyMessageAsync(
                ComponentMessageIds[key],
                x =>
                {
                    x.Components = builder.Build();
                });

            await (arg as SocketMessageComponent).DeferAsync();
        }

        protected async Task SwapBackSelectMenuItem(SocketInteraction arg, string key)
        {
            await SwapSelectMenuItem(arg, key, x => x.GetPreviousSegmentedItems());
        }

        protected async Task SwapNextSelectMenuItem(SocketInteraction arg, string key)
        {
            await SwapSelectMenuItem(arg, key, x => x.GetNextSegmentedItems());
        }
        protected async Task ResetSelectMenu(string key)
        {
            if (ComponentMessageIds.ContainsKey(key))
            {
                RestMessage msg = await ThreadChannel.GetMessageAsync(ComponentMessageIds[key]);
                List<ActionRowComponent> rows = msg.Components.ToList();
                ComponentBuilder builder = new ComponentBuilder();

                foreach (ActionRowComponent row in rows)
                {
                    ActionRowBuilder rowBuilder = new ActionRowBuilder();
                    List<IMessageComponent> comps = row.Components.ToList();
                    foreach (IMessageComponent comp in comps)
                    {
                        if (comp is ButtonComponent button)
                        {
                            rowBuilder.WithButton(
                                button.Label,
                                button.CustomId,
                                button.Style,
                                button.Emote,
                                button.Url,
                                button.IsDisabled);
                        }
                        else
                        if (comp is SelectMenuComponent menu)
                        {
                            List<SelectMenuOptionBuilder> optionBuilders = new List<SelectMenuOptionBuilder>();
                            foreach (SelectMenuOption option in menu.Options)
                            {
                                SelectMenuOptionBuilder optionBuilder = new SelectMenuOptionBuilder()
                                    .WithLabel(option.Label)
                                    .WithValue(option.Value)
                                    .WithDescription(option.Description)
                                    .WithEmote(option.Emote)
                                    .WithDefault(option.IsDefault.GetValueOrDefault());
                                optionBuilders.Add(optionBuilder);
                            }
                            rowBuilder.WithSelectMenu(
                                menu.CustomId,
                                optionBuilders,
                                menu.Placeholder,
                                menu.MinValues,
                                menu.MaxValues,
                                menu.IsDisabled,
                                menu.Type,
                                menu.ChannelTypes.ToArray());
                        }
                    }
                    builder.AddRow(rowBuilder);
                }

                await ThreadChannel.ModifyMessageAsync(ComponentMessageIds[key], x =>
                {
                    x.Components = builder.Build();
                });
            }
        }

        public override async Task SetUp()
        {
            ThreadChannel = await Scenario.CreateThread(this);
        }

        public override async Task Start() { }
    }
}
