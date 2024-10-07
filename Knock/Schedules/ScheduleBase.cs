using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Schedules
{
    public class ScheduleBase
    {
        public Guid Id { get; }
        public uint Duration { get; set; }
        public bool IsRepeat { get; set; }
        public uint RemainingTime { get; set; }
        public bool ShouldDelete { get; set; }

        public ScheduleBase(uint duration, bool isRepeat)
        {
            Duration = duration;
            Id = Guid.NewGuid();
            IsRepeat = isRepeat;
            RemainingTime = Duration;
        }

        public bool Check()
        {
            if (RemainingTime == 0)
            {
                if (IsRepeat) RemainingTime = Duration;
                else ShouldDelete = true;

                return true;
            }
            else
            {
                RemainingTime--;
                return false;
            }

        }

        public virtual Task Execute() => Task.CompletedTask;
    }
}
