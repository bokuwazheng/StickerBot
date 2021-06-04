using JournalApiClient.Data;
using System.Threading;
using System.Threading.Tasks;

namespace JournalApiClient.Services
{
    public interface IJournalApiClient
    {
        Task<string> GetStatusAsync(int id, CancellationToken ct = default);
        Task SubscribeAsync(CancellationToken ct = default);
        Task<Suggestion> CreateEntryAsync(Sender sender, string fileId, CancellationToken ct = default);
        Task<Suggestion> GetSuggestionAsync(int id, CancellationToken ct = default);
        Task<Sender> GetSenderAsync(int userId, CancellationToken ct = default);
        Task<Sender> GetSuggesterAsync(int suggestionId, CancellationToken ct = default);
        Task<Suggestion> GetNewSuggestionAsync(CancellationToken ct = default);
        Task<Sender> BanAsync(int userId, CancellationToken ct = default);
    }
}