using System;

namespace JournalApiClient.Data
{
    public record Suggestion
    {
        public int Id { get; init; }
        public string FileId { get; init; }
        public DateTime MadeAt { get; init; }
        public int UserId { get; init; }
    }
}
