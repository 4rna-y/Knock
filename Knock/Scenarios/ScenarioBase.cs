using Discord.WebSocket;
using Knock.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Scenarios
{
    public abstract class ScenarioBase
    {
        public SocketGuild Guild { get; }
        public SocketUser User { get; }
        public string ScenarioId { get; }

        protected IServiceProvider Services { get; }
        protected DataService Data { get; }
        protected LocaleService Locale { get; }
        protected ColorService Color { get; }
        protected HttpService Http { get; }
        protected ServerService Server { get; }
        protected ScheduleService Schedule { get; }
        protected ScenarioService Scenario { get; }
        protected WebSocketService WebSocket { get; }
        protected RequestService Request { get; }
        protected List<ScenarioModel> Models { get; }

        private int scenarioPos = 0;

        public ScenarioBase(SocketGuild guild, SocketUser user)
        {
            Guild = guild;
            User = user;
            ScenarioId = Utils.GetRandomString(8);
            Services = Program.GetServiceProvider();
            Data = Services.GetRequiredService<DataService>();
            Locale = Services.GetRequiredService<LocaleService>();
            Color = Services.GetRequiredService<ColorService>();
            Http = Services.GetRequiredService<HttpService>();
            Server = Services.GetRequiredService<ServerService>();
            Schedule = Services.GetRequiredService<ScheduleService>();
            Scenario = Services.GetRequiredService<ScenarioService>();
            WebSocket = Services.GetRequiredService<WebSocketService>();
            Request = Services.GetRequiredService<RequestService>();
            Models = new List<ScenarioModel>();

        }

        public abstract Task SetUp();
        public abstract Task Start();

        public virtual async Task Interact(string key, SocketInteraction arg)
        {
            if (Models.FirstOrDefault(x => x.Id == key) is ScenarioModel model)
            {
                await model.Action(arg);
            }
        }
    }
}
