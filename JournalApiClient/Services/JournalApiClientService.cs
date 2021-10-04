using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using JournalApiClient.Data;
using JournalApiClient.Data.ResponseTypes;
using Microsoft.Extensions.DependencyInjection;

namespace JournalApiClient.Services
{
    public class JournalApiClientService : IJournalApiClient
    {
        public JournalApiClientService(IGraphQLClient graphQlClient)
        {
            GraphQLClient = graphQlClient;
        }

        protected IGraphQLClient GraphQLClient { get; }

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

            var result = await GraphQLClient.SendMutationAsync<ReviewResponseType>(request, ct);
            return result.Data.AddReview;
        }

        public async Task<Sender> AddSenderAsync(Sender sender, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    mutation addSender($sender: SenderInput) {
                      addSender(sender: $sender) {
                        user_id
                        first_name
                        last_name
                        username
                        chat_id
                        is_banned
                        notify
                      }
                    }",

                Variables = new { sender },
                OperationName = "addSender"
            };

            var result = await GraphQLClient.SendQueryAsync<SenderResponseType>(request, ct);
            return result.Data.AddSender;
        }

        public async Task<Suggestion> AddSuggestionAsync(string fileId, int userId, CancellationToken ct = default)
        {
            GraphQLRequest request = new()
            {
                Query = @"
                    mutation addSuggestion($file_id: ID, $user_id: ID) {
                      addSuggestion(file_id: $file_id, user_id: $user_id) {
                        id
                        file_id
                        made_at
                        user_id
                      }
                    }",

                Variables = new { fileId, userId },
                OperationName = "addSuggestion"
            };

            var result = await GraphQLClient.SendMutationAsync<SuggestionResponseType>(request, ct);
            return result.Data.AddSuggestion;
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

            var result = await GraphQLClient.SendQueryAsync<ReviewResponseType>(request, ct);
            return result.Data.NewReview;
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

            var result = await GraphQLClient.SendQueryAsync<SuggestionResponseType>(request, ct);
            return result.Data.NewSuggestion;
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

                Variables = new { suggestionId },
                OperationName = "review"
            };

            var result = await GraphQLClient.SendQueryAsync<ReviewResponseType>(request, ct);
            return result.Data.Review;
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
                        chat_id
                        is_banned
                        notify
                      }
                    }",

                Variables = new { userId },
                OperationName = "sender"
            };

            var result = await GraphQLClient.SendQueryAsync<SenderResponseType>(request, ct);
            return result.Data.Sender;
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

            var result = await GraphQLClient.SendQueryAsync<SenderResponseType>(request, ct);
            return result.Data.Suggester;
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

                Variables = new { id },
                OperationName = "suggestion"
            };

            var result = await GraphQLClient.SendQueryAsync<SuggestionResponseType>(request, ct);
            return result.Data.Suggestion;
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

            var result = await GraphQLClient.SendMutationAsync<SenderResponseType>(request, ct);
            return result.Data.UpdateSender;
        }
    }
}
