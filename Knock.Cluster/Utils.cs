using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public static FileInfo GetFile(this DirectoryInfo directory, string filePath)
        {
            string fullPath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(directory.FullName, filePath);
            FileInfo fileInfo = new FileInfo(fullPath);

            return fileInfo;
        }

        public static DirectoryInfo GetDirectory(this DirectoryInfo directory, string filePath)
        {
            string fullPath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(directory.FullName, filePath);
            DirectoryInfo dirInfo = new DirectoryInfo(fullPath);

            return dirInfo;
        }

        public static bool Contains(this DirectoryInfo directory, string fileName)
        {
            string path = Path.Combine(directory.FullName, fileName);
            return Directory.Exists(path) | File.Exists(path);
        }

        public static string GetNonDuplicatedName(this DirectoryInfo directory, string fileName, string ext)
        {
            string suffix = "";
            string act = $"{fileName}{suffix}{ext}";
            for (int i = 1; ; i++)
            {
                FileInfo info = directory.GetFile(act);
                if (!info.Exists) break;
                suffix = $"({i})";
                act = $"{fileName}{suffix}{ext}";
            }

            return Path.Combine(directory.FullName, act);
        }

        public static string GetNonDuplicatedName(this DirectoryInfo directory, string dirName)
        {
            string suffix = "";
            string act = $"{dirName}{suffix}";
            for (int i = 1; ; i++)
            {
                DirectoryInfo info = directory.GetDirectory(act);
                if (!info.Exists) break;
                suffix = $"({i})";
                act = $"{dirName}{suffix}";
            }

            return Path.Combine(directory.FullName, act);
        }
    }
}
