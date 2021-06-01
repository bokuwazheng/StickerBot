using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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

        public async Task<Suggestion> CreateEntryAsync(int userId, string fileId, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    mutation addSuggestion($file_id: String, $user_id: ID) {
                      addSuggestion(file_id: $file_id, user_id: $user_id) {
                        file_id
                        made_at
                        user_id
                      }
                    }",

                Variables = new { file_id = fileId, user_id = userId.ToString() },
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

        public Task<string> GetStatusAsync(int userId, string fileId, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
