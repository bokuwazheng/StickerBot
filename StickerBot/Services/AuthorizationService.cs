using JournalApiClient.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StickerBot.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StickerBot.Services
{
    public class AuthorizationService : IHostedService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthorizationService> _logger;
        private readonly Jwt _jwt;
        private readonly BotOptions _botOptions;

        public AuthorizationService(ILogger<AuthorizationService> logger, IHttpClientFactory httpClientFactory, Jwt jwt, IOptions<BotOptions> options)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _botOptions = options.Value;
            _jwt = jwt;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            _logger.LogInformation("Attempting to authorize...");

            Credentials credentials = new(_botOptions.BotLogin, _botOptions.BotPassword);
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(credentials);

            ByteArrayContent content = new(bytes);
            content.Headers.ContentType = new(MediaTypeNames.Application.Json);

            Uri uri = new($"{ _botOptions.ApiBaseAddress }/login");
            using HttpRequestMessage jwtRequest = new(HttpMethod.Get, uri) { Content = content };

            using HttpResponseMessage jwtResponse = await _httpClient.SendAsync(jwtRequest, ct).ConfigureAwait(false);

            if (!jwtResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Auhtorization failed! {code}: {reason}", (int)jwtResponse.StatusCode, jwtResponse.ReasonPhrase);
                return;
            }

            Stream responseStream = await jwtResponse.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using (responseStream)
            {
                Jwt jwt = await JsonSerializer.DeserializeAsync<Jwt>(responseStream);
                _jwt.Token = jwt.Token;
            }

            _logger.LogInformation("Authorization succeeded!");
        }

        public Task StopAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
