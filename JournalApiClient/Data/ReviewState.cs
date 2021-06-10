using JournalApiClient.Data.Enums;
using System;

namespace JournalApiClient.Data
{
    public interface IReviewStateService
    {
        string FileId { get; }
        Stage Stage { get; set; }
        int UserId { get; }

        void Initialize(int userId, string fileId);
    }

    public class ReviewStateService : IReviewStateService
    {
        private ReviewState _current;

        public void Initialize(int userId, string fileId)
        {
            _current = new(userId, fileId);
        }

        public int UserId => _current.UserId;
        public string FileId => _current.FileId;
        public Stage Stage { get => _current.Stage; set => _current.Stage = value; }
    }

    public class ReviewState
    {
        public ReviewState(int userId, string fileId)
        {
            UserId = userId;
            FileId = fileId;
        }

        public Review Review { get; set; }
        public int UserId { get; }
        public string FileId { get; }
        public Stage Stage { get; set; }
    }

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

    public class ReviewPlus
    {
        public ReviewPlus() { }

        public ReviewPlus(ReviewLite reviewLite)
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
