using JournalApiClient.Data.Enums;
using System;

namespace JournalApiClient.Data
{
    public class Suggestion // TODO: Make non-nullabe.
    {
        public string FileId { get; set; }
        public DateTime? MadeAt { get; set; }
        public int? UserId { get; set; }
        public Status? Status { get; set; }
        public string Comment { get; set; }
    }
}
