﻿using JournalApiClient.Data;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JournalApiClient.Handlers
{
    public class ClientHttpMessageHandler : DelegatingHandler
    {
        private Jwt _jwt;
        private readonly ILogger<ClientHttpMessageHandler> _logger;

        public ClientHttpMessageHandler(ILogger<ClientHttpMessageHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (_jwt is null)
            {
                string baseAddress = Environment.GetEnvironmentVariable("ApiBaseAddress");
                string login = Environment.GetEnvironmentVariable("BotLogin");
                string password = Environment.GetEnvironmentVariable("BotPassword");

                object credentials = new { login, password };
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(credentials);

                ByteArrayContent content = new(bytes);
                content.Headers.ContentType = new(MediaTypeNames.Application.Json);

                Uri uri = new($"{baseAddress}/login");
                using HttpRequestMessage jwtRequest = new(HttpMethod.Get, uri) { Content = content };
                using HttpResponseMessage jwtResponse = await base.SendAsync(jwtRequest, ct).ConfigureAwait(false);

                Stream responseStream = await jwtResponse.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                await using (responseStream)
                    _jwt = await JsonSerializer.DeserializeAsync<Jwt>(responseStream);
            }

            request.Version = HttpVersion.Version20;
            request.Headers.Authorization = new("Bearer", _jwt?.Token);

            string requestJson = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            requestJson = requestJson.Replace(@"\r\n", Environment.NewLine);
            _logger.LogInformation(requestJson);

            HttpResponseMessage response = await base.SendAsync(request, ct).ConfigureAwait(false);

            string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.LogInformation(responseJson);

            if (!response.IsSuccessStatusCode)
                throw new($"{ response.StatusCode } { response.ReasonPhrase } { responseJson }");
            else
            {
                if (responseJson.Contains("error"))
                    _logger.LogError(responseJson);
            }

            return response;
        }
    }
}