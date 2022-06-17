using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lesson1
{
    public class RestClient
    {
        private readonly HttpClient client;

        public RestClient()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.174 YaBrowser/22.1.5.810 Yowser/2.5 Safari/537.36");
        }

        public async Task<string> GetStringAsync(string uri)
        {
            var result = await client.GetAsync(uri);
            for(int i = 0; i < 500 && !result.IsSuccessStatusCode; i++)
            {
                Thread.Sleep(i*1000);
                result = await client.GetAsync(uri);
                if (result.IsSuccessStatusCode) 
                    break;
            }
            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Get request failed: {uri}");
            }
            var resultedContent = await result.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(resultedContent))
            {
                throw new Exception("Nothing to return");
            }
            return resultedContent;
        }
    }
}
