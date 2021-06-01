using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using JournalApiClient.Data;
using Microsoft.Extensions.DependencyInjection;

namespace JournalApiClient.Services
{
    public class JournalApiClientService : IJournalApiClient
    {
        public JournalApiClientService(IServiceProvider provider)
        {
            HttpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            HttpClient = HttpClientFactory.CreateClient(nameof(JournalApiClientService));
            GraphQLClient = provider.GetRequiredService<IGraphQLClient>();
        }

        protected IHttpClientFactory HttpClientFactory { get; }
        protected HttpClient HttpClient { get; }
        protected IGraphQLClient GraphQLClient { get; }

        public async Task<Suggestion> CreateEntryAsync(Sender sender, string fileId, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    mutation addSuggestion($file_id: String, $sender: SenderInput) {
                      addSuggestion(file_id: $file_id, sender: $sender) {
                        file_id
                        made_at
                        user_id
                      }
                    }",

                Variables = new { file_id = fileId, sender = sender },
                OperationName = "addSuggestion"
            };

            var result = await GraphQLClient.SendMutationAsync<ResponseSuggestionType>(request, ct);
            return result.Data.AddSuggestion;
        }

        public async Task<Suggestion> GetSuggestionAsync(string fileId, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    query suggestion($file_id: ID) {
                      suggestion(file_id: $file_id) {
                        file_id
                        made_at
                        user_id
                        status
                        comment
                      }
                    }",

                Variables = new { file_id = fileId },
                OperationName = "suggestion"
            };

            var result = await GraphQLClient.SendQueryAsync<ResponseSuggestionType>(request, ct);
            return result.Data.Suggestion;
        }

        public async Task<List<Sender>> GetSendersAsync(CancellationToken ct)
        {
            GraphQLRequest request = new()
            {
                Query = @"
"
            };

            var result = await GraphQLClient.SendQueryAsync<List<Sender>>(request, ct);
            return result.Data;
        }

        public async Task<string> GetStatusAsync(string fileId, CancellationToken ct = default)
        {
            Suggestion suggestion = await GetSuggestionAsync(fileId, ct).ConfigureAwait(false);
            return $"({ suggestion.Status }) { suggestion.Comment }";
        }

        public Task SubscribeAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
