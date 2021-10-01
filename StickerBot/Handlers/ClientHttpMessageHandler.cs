using JournalApiClient.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace StickerBot.Handlers
{
    public class ClientHttpMessageHandler : DelegatingHandler
    {
        private Jwt _jwt;
        private readonly ILogger<ClientHttpMessageHandler> _logger;

        public ClientHttpMessageHandler(ILogger<ClientHttpMessageHandler> logger, Jwt jwt)
        {
            _logger = logger;
            _jwt = jwt;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            request.Version = HttpVersion.Version20;
            request.Headers.Authorization = new("Bearer", _jwt?.Token);

            string requestJson = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            requestJson = requestJson.Replace(@"\r\n", Environment.NewLine);
            _logger.LogInformation(requestJson);

            HttpResponseMessage response = await base.SendAsync(request, ct).ConfigureAwait(false);

            string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.LogInformation(responseJson);

            if (!response.IsSuccessStatusCode)
                throw new($"{ (int)response.StatusCode } { response.ReasonPhrase } { responseJson }");
            else
            {
                if (responseJson.Contains("error"))
                    _logger.LogError(responseJson);
            }

            return response;
        }
    }
}