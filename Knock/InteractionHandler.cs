using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Knock.Models;
using Knock.Scenarios;
using Knock.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Knock
{
    internal class InteractionHandler
    {
        private readonly DiscordSocketClient client;
        private readonly InteractionService handler;
        private readonly IServiceProvider services;
        private readonly IConfiguration configuration;
        private readonly LocaleService locale;
        private readonly ColorService color;
        private readonly DataService data;
        private readonly ScenarioService scenario;
        private readonly RequestService request;

        public InteractionHandler(
            DiscordSocketClient client, 
            InteractionService handler, 
            IServiceProvider services, 
            IConfiguration config,
            LocaleService locale,
            ColorService color,
            DataService data,
            ScenarioService scenario)
        {
            this.client = client;
            this.handler = handler;
            this.services = services;
            this.configuration = config;
            this.locale = locale;
            this.color = color;
            this.data = data;
            this.scenario = scenario;
        }

        public async Task Initialize()
        {
            client.Ready += OnClientReady;
            client.Disconnected += OnDisconnected;
            client.InteractionCreated += OnInteractionCreated;
            client.ButtonExecuted += OnButtonExecuted;
            client.SelectMenuExecuted += OnSelectMenuExecuted;
            client.ModalSubmitted += OnModalSubmitted;
            client.MessageReceived += OnMessageReceived;
            handler.Log += OnHandlerLog;
            handler.InteractionExecuted += OnInteractionExecuted;

            

            await handler.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }

        private async Task OnMessageReceived(SocketMessage arg)
        {
            if (arg is not IUserMessage userMessage || userMessage.Attachments.Count == 0)
                return;

            await scenario.UploadedFile(userMessage.Channel.Id, arg);
        }

        private async Task OnSelectMenuExecuted(SocketMessageComponent arg)
        {
            string[] id = arg.Data.CustomId.Split('.');
            if (id[0].Equals("scenario"))
            {
                await scenario.Interact(id[1], id[2], arg);
            }
        }

        private async Task OnModalSubmitted(SocketModal arg)
        {
            string[] id = arg.Data.CustomId.Split('.');
            if (id[0].Equals("scenario"))
            {
                await scenario.Interact(id[1], id[2], arg);
            }
        }

        private async Task OnButtonExecuted(SocketMessageComponent arg)
        {
            string[] id = arg.Data.CustomId.Split(".");
            string guildId = configuration["guild"];
            string categoryId = configuration["conversation-group"];
            SocketGuild guild = client.GetGuild(ulong.Parse(guildId));
            SocketCategoryChannel category = guild.GetCategoryChannel(ulong.Parse(categoryId));

            if (id[0].Equals("welcome"))
            {
                if (id[1].Equals("create_server"))
                {
                    if (scenario.IsInProgress<CreateServerScenario>(arg.User.Id))
                    {
                        ChannelScenarioBase s = scenario.GetScenario<CreateServerScenario>(arg.User.Id).FirstOrDefault() as ChannelScenarioBase;
                        IMessage msg = await s.TextChannel.GetMessageAsync(s.MentionMessageId);
                        EmbedBuilder embed = new EmbedBuilder()
                            .WithTitle(locale.Get("embed.scenario_created.title"))
                            .WithDescription(
                                string.Format(locale.Get("embed.scenario_created.description"),
                                msg.GetJumpUrl()))
                            .WithColor(color["warning"]);

                        await arg.RespondAsync(embed: embed.Build(), ephemeral: true);
                        return;
                    }
                    ChannelScenarioBase serverScenario = await scenario.Register(
                        new CreateServerScenario(guild, arg.User, category));

                    await arg.DeferAsync();
                }
                else
                if (id[1].Equals("load_server"))
                {
                    if (scenario.IsInProgress<LoadServerScenario>(arg.User.Id))
                    {
                        ChannelScenarioBase s = scenario.GetScenario<LoadServerScenario>(arg.User.Id).FirstOrDefault() as ChannelScenarioBase;
                        IMessage msg = await s.TextChannel.GetMessageAsync(s.MentionMessageId);
                        EmbedBuilder embed = new EmbedBuilder()
                            .WithTitle(locale.Get("embed.scenario_created.title"))
                            .WithDescription(
                                string.Format(locale.Get("embed.scenario_created.description"),
                                msg.GetJumpUrl()))
                            .WithColor(color["warning"]);

                        await arg.RespondAsync(embed: embed.Build(), ephemeral: true);
                        return;
                    }

                    await arg.DeferAsync();
                    await scenario.Register(new LoadServerScenario(guild, arg.User, category));

                }
                else
                if (id[1].Equals("reg_account"))
                {
                    if (scenario.IsInProgress<AccountRegistrationScenario>(arg.User.Id))
                    {
                        ChannelScenarioBase s = scenario.GetScenario<AccountRegistrationScenario>(arg.User.Id).FirstOrDefault() as ChannelScenarioBase;
                        IMessage msg = await s.TextChannel.GetMessageAsync(s.MentionMessageId);
                        EmbedBuilder embed = new EmbedBuilder()
                            .WithTitle(locale.Get("embed.scenario_created.title"))
                            .WithDescription(
                                string.Format(locale.Get("embed.scenario_created.description"),
                                msg.GetJumpUrl()))
                            .WithColor(color["warning"]);

                        await arg.RespondAsync(embed: embed.Build(), ephemeral: true);
                        return;
                    }

                    await arg.DeferAsync();
                    await scenario.Register(new AccountRegistrationScenario(guild, arg.User, category));
                }
            }
            else
            if (id[0].Equals("scenario"))
            {
                await scenario.Interact(id[1], id[2], arg);
            }
        }

        private Task OnDisconnected(Exception arg)
        {
            services.GetRequiredService<DataService>().Dispose();
            Console.WriteLine("[DataService] Data saved.");
            Console.WriteLine(arg.ToString());

            return Task.CompletedTask;
        }

        private async Task OnInteractionCreated(SocketInteraction interaction)
        {
            try
            {
                SocketInteractionContext ctx = new SocketInteractionContext(client, interaction);
                IResult res = await handler.ExecuteCommandAsync(ctx, services);

                if (!res.IsSuccess)
                {
                    switch (res.Error) 
                    {
                        case InteractionCommandError.UnknownCommand: break; 
                        case InteractionCommandError.ConvertFailed: break;
                        case InteractionCommandError.BadArgs: break;
                        case InteractionCommandError.Exception: break;
                        case InteractionCommandError.Unsuccessful: break;
                        case InteractionCommandError.UnmetPrecondition: break;
                        case InteractionCommandError.ParseFailed: break;
                        default: break;
                    }
                }
            }
            catch
            {
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction
                        .GetOriginalResponseAsync()
                        .ContinueWith(async msg => await msg.Result.DeleteAsync());
            }
        }

        private Task OnInteractionExecuted(ICommandInfo info, IInteractionContext ctx, IResult res)
        {
            if (!res.IsSuccess)
            {
                switch (res.Error)
                {
                    case InteractionCommandError.UnknownCommand: break;
                    case InteractionCommandError.ConvertFailed: break;
                    case InteractionCommandError.BadArgs: break;
                    case InteractionCommandError.Exception: break;
                    case InteractionCommandError.Unsuccessful: break;
                    case InteractionCommandError.UnmetPrecondition: break;
                    case InteractionCommandError.ParseFailed: break;
                    default: break;
                }
            }

            return Task.CompletedTask;
        }

        private Task OnHandlerLog(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        private async Task OnClientReady()
        {
            await handler.RegisterCommandsGloballyAsync();

            string channelId = configuration["channel"];
            if (channelId is null)
            {
                Console.WriteLine(locale.Get("error.fatal", "error.no_channel_id"));
                return;
            }

            ITextChannel channel = (await client.GetChannelAsync(ulong.Parse(channelId))) as ITextChannel;
            if (channel is null)
            {
                Console.WriteLine(locale.Get("error.fatal", "error.channel_invalid"));
                return;
            }

            ulong lastMessageId = data.Get<BotInfo>("info").LastMsg;
            if (lastMessageId != 0ul)
            {
                IMessage msg = await channel.GetMessageAsync(lastMessageId);
                if (msg is not null)
                    await msg.DeleteAsync();
            }

            EmbedBuilder eb = new EmbedBuilder()
                .WithTitle("**💽  Knock**")
                .WithColor(color["default"])
                .WithDescription(locale.Get("embed.welcome"))
                .AddField(
                    new EmbedFieldBuilder()
                        .WithName(locale.Get("embed.field1_name"))
                        .WithValue(locale.Get("embed.field1_value")))
                .WithFooter(
                    new EmbedFooterBuilder()
                        .WithText("Knock by .4rnay"));

            ComponentBuilder actionButtons = new ComponentBuilder()
                .WithButton(
                    locale.Get("button.register"),
                    "welcome.reg_account",
                    ButtonStyle.Primary,
                    new Emoji("👷"),
                    row: 0)
                .WithButton(
                    locale.Get("button.create_server"), 
                    "welcome.create_server", 
                    ButtonStyle.Primary, 
                    new Emoji("🆕"), 
                    row: 1)
                .WithButton(
                    locale.Get("button.load_server"), 
                    "welcome.load_server", 
                    ButtonStyle.Primary, 
                    new Emoji("🗂️"), 
                    row: 1);

            IMessage embedMessage = await channel.SendMessageAsync(embed: eb.Build(), components: actionButtons.Build());
            data.Set<BotInfo>("info", x =>
            {
                x.LastMsg = embedMessage.Id;
                return x;
            });
        }
    }
}
