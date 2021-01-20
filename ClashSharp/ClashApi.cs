using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ClashSharp
{
    class ClashApi
    {
        private readonly HttpClient client = new HttpClient();

        public ClashApi()
        {
            client.BaseAddress = new Uri("http://127.0.0.1:9090");
        }

        public async Task ReloadConfig()
        {
            var response = await client.PutAsync("/configs", new StringContent(""));
            response.EnsureSuccessStatusCode();
        }
    }
}
