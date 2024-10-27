using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public static Task<int> RunAsync(this Process process)
        {
            var exited = new TaskCompletionSource<int>();

            process.Exited += (sender, args) => {
                exited.TrySetResult(process.ExitCode);
            };

            if (!process.Start())
            {
                throw new InvalidOperationException($"Could not start process: {process}");
            }

            return exited.Task;
        }

        public static Task<bool> HasExitedAsync(this Process process)
        {
            var exited = new TaskCompletionSource<bool>();
            Task.Run(() => exited.TrySetResult(process.WaitForExit(TimeSpan.FromSeconds(3d))));
            return exited.Task;
        }

        public static async Task<bool> WaitForExitAsync(this Process process, int timeoutMilliseconds)
        {
            Task waitForExitTask = process.WaitForExitAsync();

            return await Task.WhenAny(waitForExitTask, Task.Delay(timeoutMilliseconds)) == waitForExitTask;
        }
    }
}
