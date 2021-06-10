using JournalApiClient.Data;
using System.Threading;
using System.Threading.Tasks;

namespace JournalApiClient.Services
{
    public interface IJournalApiClient
    {
        Task<bool> SubscribeAsync(int userId, bool notify, CancellationToken ct = default);
        Task<Suggestion> CreateSuggestionAsync(Sender sender, string fileId, CancellationToken ct = default);
        Task<Suggestion> GetSuggestionAsync(int id, CancellationToken ct = default);
        Task<Sender> GetSenderAsync(int userId, CancellationToken ct = default);
        Task<Sender> GetSuggesterAsync(int suggestionId, CancellationToken ct = default);
        Task<Suggestion> GetNewSuggestionAsync(CancellationToken ct = default);
        Task<Sender> BanAsync(int userId, CancellationToken ct = default);
        Task<Sender> UpdateSenderAsync(Sender sender, CancellationToken ct = default);
        Task<Review> AddReviewAsync(Review review, CancellationToken ct = default);
        Task<Review> GetReviewAsync(int suggestionId, CancellationToken ct = default);
    }
}