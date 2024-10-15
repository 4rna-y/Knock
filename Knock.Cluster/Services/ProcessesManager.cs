using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Cluster.Services
{
    public class ProcessesManager
    {
        private ConcurrentDictionary<Guid, Process> processes; 

        public ProcessesManager() 
        {
            processes = new ConcurrentDictionary<Guid, Process>();
        }

        public bool Create(Guid id)
        {
            
        }
    }
}
