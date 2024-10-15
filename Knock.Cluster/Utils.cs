using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Cluster
{
    public static class Utils
    {
        public static void Remove<T>(this ConcurrentBag<T> bag, T item)
        {
            foreach (T i in bag)
            {
                if (i.Equals(item)) bag.TryTake(out _);
            }
        }
    }
}
