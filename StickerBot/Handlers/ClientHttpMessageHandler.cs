using JournalApiClient.Data;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StickerBot.Handlers
{
    public class ClientHttpMessageHandler : DelegatingHandler
    {
        private readonly Jwt _jwt;
        private readonly ILogger<ClientHttpMessageHandler> _logger;

        public ClientHttpMessageHandler(ILogger<ClientHttpMessageHandler> logger, Jwt jwt)
        {
            _logger = logger;
            _jwt = jwt;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            request.Version = HttpVersion.Version20;
            request.Headers.Authorization = new("Bearer", _jwt.Token);

            string requestJson = await request.Content.ReadAsStringAsync();
            requestJson = requestJson.Replace(@"\r\n", Environment.NewLine);
            _logger.LogInformation(requestJson);

            HttpResponseMessage response = await base.SendAsync(request, ct);

            string responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(responseJson);

            if (!response.IsSuccessStatusCode)
            {
                if (response.Content.Headers.ContentType?.MediaType is MediaTypeNames.Application.Json)
                {
                    Stream responseStream = await response.Content.ReadAsStreamAsync(ct);
                    await using (responseStream)
                    {
                        using JsonDocument json = await JsonDocument.ParseAsync(responseStream, default, ct);

                        if (json.RootElement.TryGetProperty("errors", out JsonElement errors))
                        {
                            foreach (JsonElement error in errors.EnumerateArray())
                            {
                                if (error.TryGetProperty("message", out JsonElement message))
                                {
                                    throw new ApplicationException(message.GetString());
                                }
                            }
                        }
                    }
                }

                throw new($"Unknown error occured while calling { response.RequestMessage.RequestUri.PathAndQuery }: { (int)response.StatusCode } { response.ReasonPhrase }");
            }

            return response;
        }
    }
}