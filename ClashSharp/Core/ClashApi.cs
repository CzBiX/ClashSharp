using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ClashSharp.Core
{
    class ClashApi
    {
        private readonly HttpClient client = new();

        public ClashApi()
        {
            client.BaseAddress = new Uri("http://127.0.0.1:9090");
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
    }
}
