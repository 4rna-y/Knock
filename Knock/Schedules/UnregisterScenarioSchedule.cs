using Discord;
using Discord.WebSocket;
using Knock.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Schedules
{
    public class UnregisterScenarioSchedule : ScheduleBase
    {
        private string scenarioId;
        public UnregisterScenarioSchedule(string scenarioId, uint duration) : base(duration, false)
        {
            this.scenarioId = scenarioId;
        }

        public override async Task Execute()
        {
            ScenarioService service = Program.GetServiceProvider().GetRequiredService<ScenarioService>();
            await service.Unregister(scenarioId);
        }
    }
}
