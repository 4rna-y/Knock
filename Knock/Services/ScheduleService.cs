using Knock.Schedules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Knock.Services
{
    public class ScheduleService
    {
        private List<ScheduleBase> schedules;
        private Timer timer;
        public ScheduleService()
        {
            schedules = new List<ScheduleBase>();
            timer = new Timer(OnTimerTick, null, 0, 1000);
        }

        public void Resister(ScheduleBase schedule)
        {
            schedules.Add(schedule);
        }

        public void OnTimerTick(object state)
        {
            Task.Run(ExecuteSchedule);
        }

        private async Task ExecuteSchedule()
        {
            for (int i = 0; i < schedules.Count; i++)
            {
                if (schedules[i].Check())
                {
                    await schedules[i].Execute();
                    if (schedules[i].ShouldDelete) schedules.RemoveAt(i);
                }
            }
        }
    }
}
