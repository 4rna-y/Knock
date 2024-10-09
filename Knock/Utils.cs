using Discord;
using Knock.Services;
using Microsoft.Extensions.DependencyInjection;
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

        public static EmbedBuilder CreateLocalized(this EmbedBuilder builder, string keyParent)
        {
            LocaleService locale = Program.GetServiceProvider().GetRequiredService<LocaleService>();
            builder.WithTitle(locale.Get(keyParent + ".title"));

            string desc = locale.Get(keyParent + ".description");
            if (!string.IsNullOrWhiteSpace(desc))
            {
                builder.WithDescription(desc);
            }

            return builder;
        }
    }
}
