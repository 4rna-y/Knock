using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Scenarios
{
    public class ScenarioModel
    {
        public string Id { get; }
        public Func<SocketInteraction, Task> Action { get; }
        public ScenarioModel(string id, Func<SocketInteraction, Task> action)
        {
            Id = id;
            Action = action;
        }
    }
}
