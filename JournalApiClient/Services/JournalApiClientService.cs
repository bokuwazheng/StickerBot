using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using JournalApiClient.Data;
using JournalApiClient.Data.Enums;
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
                        id
                        file_id
                        made_at
                        user_id
                        status
                        comment
                      }
                    }",

                Variables = new { FileId = fileId, Sender = sender },
                OperationName = "addSuggestion"
            };

            var result = await GraphQLClient.SendMutationAsync<ResponseSuggestionType>(request, ct).ConfigureAwait(false);
            return result.Data.AddSuggestion;
        }

        public async Task<Suggestion> GetSuggestionAsync(int id, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    query suggestion($id: ID) {
                      suggestion(id: $id) {
                        file_id
                        made_at
                        user_id
                        status
                        comment
                      }
                    }",

                Variables = new { id = id },
                OperationName = "suggestion"
            };

            var result = await GraphQLClient.SendQueryAsync<ResponseSuggestionType>(request, ct).ConfigureAwait(false);
            return result.Data.Suggestion;
        }

        public async Task<Sender> GetSenderAsync(int userId, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    query sender($user_id: ID) {
                      sender(user_id: $user_id) {
                        user_id
                        first_name
                        last_name
                        username
                        is_banned
                        notify
                      }
                    }",

                Variables = new { user_id = userId },
                OperationName = "sender"
            };

            var result = await GraphQLClient.SendQueryAsync<ResponseSenderType>(request, ct).ConfigureAwait(false);
            return result.Data.Sender;
        }

        public async Task<Suggestion> GetNewSuggestionAsync(CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    query suggestion() {
                      suggestion() {
                        file_id
                        made_at
                        user_id
                        status
                        comment
                      }
                    }",

                OperationName = "suggestion"
            };

            var result = await GraphQLClient.SendQueryAsync<ResponseSuggestionType>(request, ct).ConfigureAwait(false);
            return result.Data.Suggestion;
        }

        public async Task<List<Sender>> GetSendersAsync(CancellationToken ct)
        {
            GraphQLRequest request = new()
            {
                Query = @"
"
            };

            var result = await GraphQLClient.SendQueryAsync<List<Sender>>(request, ct).ConfigureAwait(false);
            return result.Data;
        }

        public async Task<string> GetStatusAsync(int id, CancellationToken ct = default)
        {
            Suggestion suggestion = await GetSuggestionAsync(id, ct).ConfigureAwait(false);
            return $"({ suggestion.Status }) { suggestion.Comment }";
        }

        public async Task<bool> SubscribeAsync(int userId, bool notify, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    mutation subscribe($user_id: ID, $notify: Boolean) {
                      subscribe(user_id: $user_id, notify: $notify) {
                        notify
                      }
                    }",

                Variables = new { user_id = userId, notify = notify },
                OperationName = "subscribe"
            };

            var result = await GraphQLClient.SendMutationAsync<ResponseSenderType>(request, ct).ConfigureAwait(false);
            return result.Data.Subscribe.Notify;
        }

        public async Task<Sender> BanAsync(int suggestionId, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    mutation banSender($suggestion_id: ID) {
                      banSender(suggestion_id: $suggestion_id) {
                        username
                      }
                    }",

                Variables = new { suggestion_id = suggestionId },
                OperationName = "banSender"
            };

            var result = await GraphQLClient.SendMutationAsync<ResponseSenderType>(request, ct).ConfigureAwait(false);
            return result.Data.BanSender;
        }

        public async Task<Sender> GetSuggesterAsync(int suggestionId, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    query suggester($suggestion_id: ID) {
                      suggester(suggestion_id: $suggestion_id) {
                        user_id
                        first_name
                        last_name
                        username
                        is_banned
                        chat_id
                        notify
                      }
                    }",

                Variables = new { suggestion_id = suggestionId },
                OperationName = "suggester"
            };

            var result = await GraphQLClient.SendQueryAsync<ResponseSenderType>(request, ct).ConfigureAwait(false);
            return result.Data.Suggester;
        }
    }
}
