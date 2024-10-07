using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Knock.Cluster.Services
{
    public class HttpService
    {
        private readonly HttpClient http;
        public HttpService() 
        {
            http = new HttpClient();
        }

        public async Task Download(string url, DirectoryInfo dir, string name)
        {
            using HttpResponseMessage res = await http.GetAsync(url);
            using FileStream file = new FileStream(Path.Combine(dir.FullName, name), FileMode.Create);
            res.EnsureSuccessStatusCode();
            await res.Content.CopyToAsync(file);
        }
    }
}
