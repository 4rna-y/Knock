using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Services
{
    public class LogService
    {
        private readonly IConfiguration config;
        private readonly DiscordSocketClient client;

        private ITextChannel channel;

        public LogService(
            IConfiguration config,
            DiscordSocketClient client)
        {
            this.config = config;
            this.client = client;
        }

        public async Task Start()
        {
            channel = (ITextChannel)await client.GetChannelAsync(ulong.Parse(config["log"]));
        }

        public async Task Info(string text)
        {
            await channel.SendMessageAsync("**[Info]** " + text);
        }

        public async Task Error(string text)
        {
            await channel.SendMessageAsync("**[Error]** " + text);
        }
    }
}
