using Knock.Cluster.Networks;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Cluster.Services
{
    public class CommandService
    {
        private readonly ILogger logger;
        private readonly WebSocketHandler handler;
        private readonly ContainerService container;

        private bool isRunning;
        private Dictionary<string, Func<Task>> commands;

        public CommandService(
            ILogger logger,
            WebSocketHandler handler,
            ContainerService container)
        {
            this.logger = logger;
            this.handler = handler;
            this.container = container;

            logger.Info("CommandService has been initialized.");
            commands = new Dictionary<string, Func<Task>>()
            {
                { "open", OnOpen },
                { "close", OnClose },
                { "quit", OnQuit },
                { "test", OnTest }
            };
        }

        public void Start()
        {            
            isRunning = true;
            while (isRunning)
            {
                string input = Console.ReadLine();
                if (commands.TryGetValue(input, out Func<Task> action)) action();
                else logger.Warn($"No commands were found for {input}.");
            }
        }

        private async Task OnOpen()
        {
            await handler.Start();
        }

        private async Task OnClose()
        {
            await handler.Close();
        }

        private Task OnQuit()
        {
            isRunning = false;
            return Task.CompletedTask;
        }

        private async Task OnTest()
        {
           

            logger.Info("test complated");
        }
    }
}
