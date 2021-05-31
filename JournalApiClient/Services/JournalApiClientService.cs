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

        private static HttpRequestMessage CreateHttpRequestMessage(HttpMethod httpMethod, string apiMethod, List<KeyValuePair<string, string>> queryParams = null, HttpContent content = null)
        {
            Uri uri;
            if (queryParams?.Count > 0)
            {
                NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
                queryParams.ForEach(pair =>
                {
                    if (pair.Value is not null)
                        query.Add(pair.Key, pair.Value);
                });
                uri = new Uri($"{apiMethod}?{query}", UriKind.Relative);
            }
            else
                uri = new(apiMethod, UriKind.Relative);
            return new(httpMethod, uri)
            {
                Content = content,
            };
        }

        public async Task<Jwt> GetJwtAsync(string login, string password, CancellationToken ct)
        {
            object credentials = new { login, password };
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(credentials);

            ByteArrayContent content = new(bytes);
            content.Headers.ContentType = new(MediaTypeNames.Application.Json);

            Jwt jwt = null;
            Uri uri = new("/login", UriKind.Relative);
            using HttpRequestMessage request = new(HttpMethod.Get, uri) { Content = content };
            using HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);

            Stream responseStream = await response.Content.ReadAsStreamAsync(ct);
            await using (responseStream) 
                jwt = await JsonSerializer.DeserializeAsync<Jwt>(responseStream);

            return jwt;
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
