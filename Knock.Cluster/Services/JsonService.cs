using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Knock.Cluster.Services
{
    public class JsonService
    {
        public JsonService() 
        {

        }

        public async Task CreateFile<T>(T value, DirectoryInfo dir, string name)
        {
            FileInfo file = new FileInfo(Path.Combine(dir.FullName, name));
            if (file.Exists) file.Delete();

            using (TextWriter tw = file.CreateText())
            {
                string str = JsonSerializer.Serialize(value);
                await tw.WriteAsync(str);
                await tw.FlushAsync();
                tw.Close();
            }
        }

        public async Task<T> LoadFile<T>(DirectoryInfo dir, string name)
        {
            FileInfo file = new FileInfo(Path.Combine(dir.FullName, name));

            using (TextReader tr = file.OpenText())
            {
                string str = await tr.ReadToEndAsync();
                return JsonSerializer.Deserialize<T>(str);
            }
        }
    }
}
