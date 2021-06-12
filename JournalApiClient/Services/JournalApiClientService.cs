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

        public async Task<Suggestion> CreateSuggestionAsync(Sender sender, string fileId, CancellationToken ct = default)
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
                        id                        
                        file_id
                        made_at
                        user_id
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
                        chat_id
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
                    query newSuggestion {
                      newSuggestion {
                        id
                        file_id
                        made_at
                        user_id
                      }
                    }",

                OperationName = "newSuggestion"
            };

            var result = await GraphQLClient.SendQueryAsync<ResponseSuggestionType>(request, ct).ConfigureAwait(false);
            return result.Data.NewSuggestion;
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

        public async Task<Review> GetReviewAsync(int suggestionId, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    query review($suggestion_id: ID) {
                      review(suggestion_id: $suggestion_id) {
                        id
                        suggestion_id
                        user_id
                        submitted_at
                        result_code
                      }
                    }",

                Variables = new { SuggestionId = suggestionId },
                OperationName = "review"
            };

            var result = await GraphQLClient.SendQueryAsync<ReviewResponseType>(request, ct).ConfigureAwait(false);
            return result.Data.Review;
        }

        public async Task<Review> GetNewReviewAsync(int userId, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    query newReview($user_id: ID) {
                      newReview(user_id: $user_id) {
                        id
                        suggestion_id
                        user_id
                        submitted_at
                        result_code
                      }
                    }",

                Variables = new { UserId = userId },
                OperationName = "newReview"
            };

            var result = await GraphQLClient.SendQueryAsync<ReviewResponseType>(request, ct).ConfigureAwait(false);
            return result.Data.NewReview;
        }

        public async Task<bool> SubscribeAsync(int userId, bool notify, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    mutation subscribe($user_id: ID, $notify: Boolean) {
                      subscribe(user_id: $user_id, notify: $notify) {
                        user_id
                        first_name
                        last_name
                        username
                        is_banned
                        chat_id
                        notify
                      }
                    }",

                Variables = new { user_id = userId, notify = notify },
                OperationName = "subscribe"
            };

            var result = await GraphQLClient.SendMutationAsync<ResponseSenderType>(request, ct).ConfigureAwait(false);
            return result.Data.Subscribe.Notify;
        }

        public async Task<Sender> UpdateSenderAsync(Sender sender, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    mutation updateSender($sender: SenderInput) {
                      updateSender(sender: $sender) {
                        user_id
                        first_name
                        last_name
                        username
                        is_banned
                        chat_id
                        notify
                      }
                    }",

                Variables = new { sender },
                OperationName = "updateSender"
            };

            var result = await GraphQLClient.SendMutationAsync<ResponseSenderType>(request, ct).ConfigureAwait(false);
            return result.Data.UpdateSender;
        }

        public async Task<Sender> BanAsync(int suggestionId, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    mutation banSender($suggestion_id: ID) {
                      banSender(suggestion_id: $suggestion_id) {
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

        public async Task<Review> AddReviewAsync(Review review, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    mutation addReview($review: ReviewInput) {
                      addReview(review: $review) {
                        id
                        suggestion_id
                        user_id
                        submitted_at
                        result_code
                      }
                    }",

                Variables = new { review },
                OperationName = "addReview"
            };

            var result = await GraphQLClient.SendMutationAsync<ReviewResponseType>(request, ct).ConfigureAwait(false);
            return result.Data.AddReview;
        }
    }
}
