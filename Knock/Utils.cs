using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knock
{
    public static class Utils
    {
        public static string GetRandomString(int length)
        {
            string key = "abcdefghijklmnopqrstuvwxyz1234567890";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append(key[Random.Shared.Next(0, key.Length - 1)]);
            }

            return sb.ToString();
        }

        public static void Remove<T>(this ConcurrentBag<T> bag, T item)
        {
            foreach (T i in bag)
            {
                if (i.Equals(item)) bag.TryTake(out _);
            }
        }
    }
}
