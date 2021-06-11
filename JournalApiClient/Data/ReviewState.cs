using JournalApiClient.Data.Enums;
using System;

namespace JournalApiClient.Data
{
    public class Review
    {
        public Review() { }

        public Review(ReviewLite reviewLite) 
        {
            SuggestionId = reviewLite.Id;
            UserId = reviewLite.By;
            ResultCode = reviewLite.Result;
        }

        public int Id { get; set; }
        public int SuggestionId { get; set; }
        public int UserId { get; set; }
        public DateTime SubmittedAt { get; set; }

        public ReviewResult ResultCode { get; set; }
    }

    public class ReviewLite
    {
        public ReviewLite() { }

        public ReviewLite(int suggestionId, int userId) => (Id, By) = (suggestionId, userId);

        public int Id { get; set; }
        public int By { get; set; }
        public ReviewResult Result { get; set; }
    }
}
