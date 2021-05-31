using JournalApiClient.Data;
using System.Threading;
using System.Threading.Tasks;

namespace JournalApiClient.Services
{
    public interface IJournalApiClient
    {
        Task<Jwt> GetJwtAsync(string login, string password, CancellationToken ct = default);
        Task<string> GetStatusAsync(int userId, string fileId, CancellationToken ct = default);
        Task<Suggestion> CreateEntryAsync(int userId, string fileId, CancellationToken ct = default);
        Task<Suggestion> GetSuggestionAsync(string fileId, CancellationToken ct = default);
    }
}