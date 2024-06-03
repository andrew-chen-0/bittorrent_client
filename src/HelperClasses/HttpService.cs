using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace codecrafters_bittorrent
{
    internal class HttpService
    {
        private readonly HttpClient _client;

        public HttpService()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All
            };

            _client = new HttpClient();
        }

        private static string QueryString(IDictionary<string, object> dict)
        {
            var list = new List<string>();
            foreach (var item in dict)
            {
                list.Add(item.Key + "=" + item.Value);
            }
            return string.Join("&", list);
        }

        public async Task<byte[]> GetAsync(string uri, Dictionary<string, object>? parameters = null)
        {
            if (parameters != null)
            {
                uri += "?" + QueryString(parameters);
            }

            using HttpResponseMessage response = await _client.GetAsync(uri);

            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<string> PostAsync(string uri, string data, string contentType)
        {
            using HttpContent content = new StringContent(data, Encoding.UTF8, contentType);

            HttpRequestMessage requestMessage = new HttpRequestMessage()
            {
                Content = content,
                Method = HttpMethod.Post,
                RequestUri = new Uri(uri)
            };

            using HttpResponseMessage response = await _client.SendAsync(requestMessage);

            return await response.Content.ReadAsStringAsync();
        }
    }
}
