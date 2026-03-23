using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Colyseus
{
    public class DefaultHttpClient : IHttpClient
    {
        private static readonly HttpClient _client = new HttpClient();

        public async Task<string> Request(string method, string url, byte[] body, Dictionary<string, string> headers)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), url);

            if (body != null)
            {
                request.Content = new ByteArrayContent(body);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            var response = await _client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpException((int)response.StatusCode, responseBody);
            }

            return responseBody;
        }
    }
}
