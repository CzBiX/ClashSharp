using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClashSharp.Core
{
    class ClashApi
    {
        public const string BaseAddr = "127.0.0.1:9090";
        private readonly HttpClient client = new();

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public ClashApi()
        {
            client.BaseAddress = new Uri("http://" + BaseAddr);
        }

        public async Task ReloadConfig(string path)
        {
            var dict = new Dictionary<string, string>()
            {
                {"path", path},
            };
            var content = JsonContent.Create(dict);
            var response = await client.PutAsync("/configs", content);
            response.EnsureSuccessStatusCode();
        }

        public record VersionInfo
        {
            public string Version { get; set; } = default!;
            public bool Premium { get; set; } = default;
        }

        public async Task<VersionInfo> GetVersion()
        {
            var response = await client.GetFromJsonAsync<VersionInfo>("/version", JsonOptions);

            return response!;
        }
    }
}
