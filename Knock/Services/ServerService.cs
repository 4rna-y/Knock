using Knock.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Knock.Services
{
    public class ServerService
    {
        private readonly DataService data;

        private ConcurrentBag<Guid> lockedServer;
        private ConcurrentDictionary<string, ServerContainerBuilder> builders;

        public ServerService(DataService data) 
        {
            this.data = data;

            lockedServer = new ConcurrentBag<Guid>();
            builders = new ConcurrentDictionary<string, ServerContainerBuilder>();
        }

        public ServerContainer CreateContainer(string id)
        {
            ServerContainer container = GetBuilder(id).Build();
            data.Set<ServerContainers>("containers", x =>
            {
                x.Containers.Add(container);
                return x;
            });

            RemoveBuilder(id);
            return container;
        }

        public ServerContainer GetContainer(Guid id)
        {
            return data.Get<ServerContainers>("containers").Containers.FirstOrDefault(x => x.Id.Equals(id));
        }

        public bool RemoveContainer(Guid id)
        {
            if (IsLocked(id)) return false;
            data.Set<ServerContainers>("containers", x =>
            {
                x.Containers.Remove(x.Containers.FirstOrDefault(y => y.Id.Equals(id)));
                return x;
            });

            return true;
        }

        public bool IsLocked(Guid id)
        {
            return lockedServer.Any(x => x.Equals(id));
        }

        public void Lock(Guid id)
        {
            lockedServer.Add(id);
        }

        public void Unlock(Guid id)
        {
            lockedServer.Remove(id);
        } 

        public void CreateBuilder(string id)
        {
            if (builders.ContainsKey(id)) return;

            builders[id] = new ServerContainerBuilder();    
        }

        public ServerContainerBuilder GetBuilder(string id)
        {
            builders.TryGetValue(id, out var builder);
            return builder;
        }

        public void SetBuilder(string id, Func<ServerContainerBuilder, ServerContainerBuilder> action)
        {
            ServerContainerBuilder src = GetBuilder(id);
            ServerContainerBuilder dest = action(src);
            builders.TryUpdate(id, dest, src);
        }

        public void RemoveBuilder(string id)
        {
            builders.TryRemove(id, out _);
        }
    }
}
