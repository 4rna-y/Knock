using Knock.Cluster.Networks;
using Knock.Cluster.Services;
using Knock.Transport;
using Knock.Transport.Enum;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NLog;
using Spectre.Console;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Cluster
{
    public class Program
    {
        private static IConfiguration config;
        private static IServiceProvider serviceProvider;
        
        static async Task Main(string[] args)
        {
            config = new ConfigurationBuilder()
                .AddJsonFile("appsetting.json", false, true)
                .Build();
            serviceProvider = new ServiceCollection()
                .AddSingleton(config)
                .AddSingleton<ILogger>(x => LogManager.GetCurrentClassLogger())
                .AddSingleton<HttpService>()
                .AddSingleton<JsonService>()
                .AddSingleton<ContainerService>()
                .AddSingleton<DataProcesser>()
                .AddSingleton<WebSocketHandler>()
                .AddSingleton<ResponseHandler>()
                .AddSingleton<CommandService>()
                .AddSingleton<JavaProcessProvider>()
                .AddSingleton<ProcessesManager>()
                .BuildServiceProvider();

            JavaProcessProvider jProvider = serviceProvider.GetRequiredService<JavaProcessProvider>();
            await jProvider.Setup();


            CommandService cmd = serviceProvider.GetRequiredService<CommandService>();

            cmd.Start();
        }
    }
}
