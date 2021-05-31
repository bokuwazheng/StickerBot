using JournalApiClient.Data;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JournalApiClient.Handlers
{
    public class ClientHttpMessageHandler : DelegatingHandler
    {
        private Jwt _jwt;

        public ClientHttpMessageHandler(Jwt jwt) => _jwt = jwt;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            request.Version = HttpVersion.Version20;
            //request.Headers.Authorization = new("Bearer", _jwt.Token);
            string result = await request.Content.ReadAsStringAsync();
            HttpResponseMessage response = await base.SendAsync(request, ct).ConfigureAwait(true);
            //string result2 = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Something went terribly wrong!");

            return response;
        }
    }
}
