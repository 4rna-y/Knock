using Discord.WebSocket;
using Knock.Schedules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Scenarios
{
    public class ServerContainerLogOutputThreadScenario : ThreadScenarioBase
    {
        private Guid containerId;
        private LogSchedule log;

        public ServerContainerLogOutputThreadScenario(ChannelScenarioBase channelScenario, string name, Guid containerId) : 
            base(channelScenario, name, "log-output", channelScenario.Guild, channelScenario.User)
        {
            this.containerId = containerId;
        }

        public override Task Start()
        {
            log = new LogSchedule(containerId, this.ThreadChannel);
            Schedule.Resister(log);
            return Task.CompletedTask;
        }

        public void OnDelete()
        {
            Schedule.Unregister(log);
        }
    }
}
