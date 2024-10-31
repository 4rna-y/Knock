using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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

        public async Task<string> Download(string url, DirectoryInfo dir, string name)
        {
            using HttpResponseMessage res = await http.GetAsync(url);
            using FileStream file = new FileStream(Path.Combine(dir.FullName, name), FileMode.Create);
            res.EnsureSuccessStatusCode();
            await res.Content.CopyToAsync(file);
            return Path.Combine(dir.FullName, name);
        }

        public async Task<HttpResponseModel<JsonDocument>> Get(string url)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            using (HttpResponseMessage response = await http.SendAsync(request))
            {
                string resText = await response.Content.ReadAsStringAsync();
                JsonDocument json;

                try
                {
                    json = JsonDocument.Parse(resText);
                }
                catch
                {
                    json = null;
                }
                HttpResponseModel<JsonDocument> res = new HttpResponseModel<JsonDocument>(response.StatusCode, json);

                return res;
            }
        }
    }
}
