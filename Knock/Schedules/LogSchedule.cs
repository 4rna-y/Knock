using Discord.Rest;
using Knock.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Schedules
{
    public class LogSchedule : ScheduleBase
    {
        private readonly RequestService request;
        private Guid id;
        private RestThreadChannel channel;
        public LogSchedule(Guid id, RestThreadChannel channel) : base(2, true)
        {
            this.id = id;
            this.channel = channel;
            request = Program.GetServiceProvider().GetRequiredService<RequestService>();
        }

        public override async Task Execute()
        {
            string log = await request.GetLog(id);
            if (string.IsNullOrWhiteSpace(log)) return;
            await channel.SendMessageAsync(log);
        }
    }
}
