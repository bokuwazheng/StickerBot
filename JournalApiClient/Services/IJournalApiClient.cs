using JournalApiClient.Data;
using System.Threading;
using System.Threading.Tasks;

namespace JournalApiClient.Services
{
    public interface IJournalApiClient
    {
        Task<string> GetStatusAsync(string fileId, CancellationToken ct = default);
        Task SubscribeAsync(CancellationToken ct = default);
        Task<Suggestion> CreateEntryAsync(Sender sender, string fileId, CancellationToken ct = default);
        Task<Suggestion> GetSuggestionAsync(string fileId, CancellationToken ct = default);
        Task<Suggestion> GetNewSuggestionAsync(CancellationToken ct = default);
    }
}