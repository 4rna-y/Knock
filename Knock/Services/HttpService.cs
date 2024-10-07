using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CmlLib.Core.Installer.Forge.Versions;
using Knock.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Knock.Services
{
    public class HttpService
    {
        private readonly HttpClient http;
        public HttpService() 
        {
            http = new HttpClient();
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

        public async Task<HttpResponseModel<JsonDocument>> GetPaperVersions()
        {
            string url = "https://api.papermc.io/v2/projects/paper";
            return await Get(url);
        }

        public async Task<HttpResponseModel<JsonDocument>> GetPaperBuildNumber(string version)
        {
            string url = $"https://api.papermc.io/v2/projects/paper/versions/{version}";
            return await Get(url);
        }

        public async Task<string> GetPaperDownloadLink(string version, string build)
        {
            string url =
                $"https://api.papermc.io/v2/projects/paper/versions/" +
                $"{version}/builds/{build}/downloads/paper-{version}-{build}.jar";

            using (HttpResponseMessage res = await http.GetAsync(url))
            {
                if (!res.IsSuccessStatusCode) return null;
            }

            return url;
        }

        public async Task<List<string>> GetForgeVersions()
        {
            string url = "https://files.minecraftforge.net/net/minecraftforge/forge/";
            string html = await http.GetStringAsync(url);
            
            HtmlParser parser = new HtmlParser();
            IHtmlDocument document = parser.ParseDocument(html);
            IHtmlCollection<IElement> elements = document.Body.QuerySelectorAll("ul.nav-collapsible");

            List<string> list = new List<string>();
            foreach (var e in elements)
            {
                foreach (var item in e.TextContent.Split("\n"))
                {
                    string i = item.Trim().Replace("\n", "");
                    if (string.IsNullOrWhiteSpace(i)) continue;

                    if (int.TryParse(i.Split(".")[1], out int res))
                    {
                        if (res > 5) list.Add(i);
                    }
                }
            }

            return list;
        }

        public async Task<ForgeVersion> GetForgeVersion(string version)
        {
            ForgeVersionLoader l = new ForgeVersionLoader(http);
            IEnumerable<ForgeVersion> vers = await l.GetForgeVersions(version);
            if (!vers.Any()) return null;

            foreach (ForgeVersion ver in vers)
            {
                if (ver.IsRecommendedVersion) return ver;
            }

            foreach (ForgeVersion ver in vers)
            {
                if (ver.IsLatestVersion) return ver;
            }

            return null;
        }


    }
}
