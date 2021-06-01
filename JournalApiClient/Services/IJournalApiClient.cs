using JournalApiClient.Data;
using System.Threading;
using System.Threading.Tasks;

namespace JournalApiClient.Services
{
    public interface IJournalApiClient
    {
        Task<string> GetStatusAsync(int userId, string fileId, CancellationToken ct = default);
        Task<Suggestion> CreateEntryAsync(int userId, string fileId, CancellationToken ct = default);
        Task<Suggestion> GetSuggestionAsync(string fileId, CancellationToken ct = default);
    }
}