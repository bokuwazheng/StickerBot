using JournalApiClient.Data.Enums;
using System;

namespace JournalApiClient.Data
{
    public record Review
    {
        public Review() { }

        public Review(ReviewLite reviewLite)
        {
            SuggestionId = reviewLite.SuggestionId;
            UserId = reviewLite.SuggesterId;
            ResultCode = reviewLite.Result;
        }

        public int Id { get; init; }
        public int SuggestionId { get; init; }

        /// <summary>
        /// User that created the review.
        /// </summary>
        public int UserId { get; init; }
        public DateTime SubmittedAt { get; init; }
        public ReviewResult ResultCode { get; init; }
    }
}
