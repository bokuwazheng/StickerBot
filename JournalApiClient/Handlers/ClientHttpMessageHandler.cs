using JournalApiClient.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JournalApiClient.Handlers
{
    public class ClientHttpMessageHandler : DelegatingHandler
    {
        private IJournalApiClient _journalApiClient;
        private string _jwt; // TODO: Get JWT somewhere else.

        //public ClientHttpMessageHandler(IJournalApiClient journalApiClient)
        //{
        //    _journalApiClient = journalApiClient;
        //}

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            //if (_jwt is null)
            //    _jwt = await _journalApiClient.GetJwtAsync(GetEV("BotLogin"), GetEV("BotPassword"), ct);
            
            request.Version = HttpVersion.Version20;
            //request.Headers.Authorization = new("Bearer", _jwt);
            //string result = await request.Content.ReadAsStringAsync();
            HttpResponseMessage response = await base.SendAsync(request, ct).ConfigureAwait(true);
            //string result2 = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Something went terribly wrong!");

            return response;
        }

        private static string GetEV(string key) => Environment.GetEnvironmentVariable(key);
    }
}
