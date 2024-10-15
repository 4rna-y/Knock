using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Knock.Scenarios;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Services
{
    public class ScenarioService
    {
        private readonly IConfiguration config;
        private readonly LocaleService locale;

        private Dictionary<string, ScenarioBase> scenarios;
        private Dictionary<string, RestTextChannel> registeredChannels;

        public ScenarioService(IConfiguration config, LocaleService locale)
        {
            this.config = config;
            this.locale = locale;

            registeredChannels = new Dictionary<string, RestTextChannel>();
            scenarios = new Dictionary<string, ScenarioBase>();
        }

        public bool IsInProgress<T>(ulong user) where T : ScenarioBase
        {
            return scenarios.Values.Any(x => x.User.Id.Equals(user) && x.GetType().Name.Equals(typeof(T).Name));
        }

        public IEnumerable<ScenarioBase> GetScenario<T>(ulong user) where T: ScenarioBase
            => scenarios.Values.Where(x => x.User.Id.Equals(user) && x.GetType().Name.Equals(typeof(T).Name));

        public async Task<T> Register<T>(T scenario) where T : ScenarioBase
        {
            scenarios.Add(scenario.ScenarioId, scenario);
            await scenario.SetUp();
            await scenario.Start();

            return scenario;
        }

        public async Task<RestTextChannel> CreateChannel(ChannelScenarioBase channelScenario)
        {
            RestTextChannel t = await channelScenario.Guild.CreateTextChannelAsync(channelScenario.Name, x => x.CategoryId = channelScenario.Category.Id);

            await t.AddPermissionOverwriteAsync(channelScenario.Guild.EveryoneRole, OverwritePermissions.DenyAll(t));
            await t.AddPermissionOverwriteAsync(channelScenario.User, OverwritePermissions.AllowAll(t));

            registeredChannels.Add(channelScenario.ScenarioId, t);

            ComponentBuilder builder = new ComponentBuilder()
                .WithButton(
                    locale.Get("button.close_scenario"),
                    $"scenario.{channelScenario.ScenarioId}.close-scenario",
                    ButtonStyle.Secondary,
                    new Emoji("👋"));

            IMessage msg = await t.SendMessageAsync($"{channelScenario.User.Mention}", components: builder.Build());
            channelScenario.MentionMessageId = msg.Id;

            return t;
        }

        public async Task<RestThreadChannel> CreateThread(ThreadScenarioBase threadScenario)
        {
            RestThreadChannel thread =
                await threadScenario.ChannelScenario.TextChannel.CreateThreadAsync(
                    threadScenario.Name,
                    ThreadType.PrivateThread,
                    ThreadArchiveDuration.OneHour);

            await thread.AddUserAsync(threadScenario.Guild.Users.FirstOrDefault(x => x.Id.Equals(threadScenario.User.Id)));
            threadScenario.ChannelScenario.Threads.Add(threadScenario.Key, thread.Id);
            return thread;
        }

        public async Task DeleteThread(ThreadScenarioBase threadScenario)
        {
            await threadScenario.ThreadChannel.DeleteAsync();
            threadScenario.ChannelScenario.Threads.Remove(threadScenario.Key);
            scenarios.Remove(threadScenario.ScenarioId);
        }

        public async Task Interact(string id, string name, SocketInteraction arg)
        {
            if (!scenarios.ContainsKey(id)) return;
            await scenarios[id].Interact(name, arg);
        }

        public async Task Unregister(string id)
        {
            if (registeredChannels.ContainsKey(id))
            {
                await registeredChannels[id].DeleteAsync();
            }
            scenarios.Remove(id);
            registeredChannels.Remove(id);
        }

        public async Task ChangeChannelName(ChannelScenarioBase scenario, string name)
        {
            await scenario.TextChannel.ModifyAsync(x => x.Name = name);
        }

        public async Task Dispose()
        {
            foreach (RestTextChannel channel in registeredChannels.Values)
            {
                await channel.DeleteAsync();
            }
        }
    }
}
