using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Knock.Services;
using Knock.Models;
using Knock.Transport;

namespace Knock
{
    internal class Program
    {
        private static IConfiguration configuration;
        private static IServiceProvider services;

        public static IServiceProvider GetServiceProvider() => services;

        private static readonly DiscordSocketConfig socketConfig = new()
        {
            GatewayIntents = GatewayIntents.All,
            AlwaysDownloadUsers = true,
        };

        static async Task Main(string[] args)
        {
            try
            {
                configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsetting.json", false, true)
                    .Build();
                services = new ServiceCollection()
                    .AddSingleton(configuration)
                    .AddSingleton(socketConfig)
                    .AddSingleton<DiscordSocketClient>()
                    .AddSingleton(
                        x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                    .AddSingleton<LogService>()
                    .AddSingleton<InteractionHandler>()
                    .AddSingleton<LocaleService>()
                    .AddSingleton<ColorService>()
                    .AddSingleton<DataService>()
                    .AddSingleton<ScenarioService>()
                    .AddSingleton<HttpService>()
                    .AddSingleton<ServerService>()
                    .AddSingleton<DataProcesser>()
                    .AddSingleton<WebSocketService>()
                    .AddSingleton<RequestService>()
                    .AddSingleton<ScheduleService>()
                    .AddSingleton<ServerPropertiesProvider>()
                    .BuildServiceProvider();

                services.GetRequiredService<LocaleService>().Language = configuration["locale"] ?? "en";

                DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();
                LocaleService locale = services.GetRequiredService<LocaleService>();
                DataService data = services.GetRequiredService<DataService>();
                RequestService request = services.GetRequiredService<RequestService>();
                LogService log = services.GetRequiredService<LogService>();

                client.Log += OnLog;
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

                await services.GetRequiredService<InteractionHandler>().Initialize();
                services.GetRequiredService<WebSocketService>().Start();
                

                if (configuration["token"] is string token && !string.IsNullOrWhiteSpace(token))
                {
                    await client.LoginAsync(TokenType.Bot, token);
                    await client.StartAsync();
                    await log.Start();

                    await Task.Delay(Timeout.Infinite);
                }
                else
                {

                    Console.WriteLine(locale.Get("error.fatal", "error.no_token"));
                    return;
                }

                

            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            services.GetRequiredService<ScenarioService>().Dispose().GetAwaiter().GetResult();
            services.GetRequiredService<DataService>().Dispose();
        }

        private static Task OnLog(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
    }
}
