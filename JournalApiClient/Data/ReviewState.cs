using JournalApiClient.Data.Enums;

namespace JournalApiClient.Data
{
    public interface IReviewStateService
    {
        string FileId { get; }
        Review State { get; set; }
        int UserId { get; }

        void Initialize(int userId, string fileId);
    }

    public class ReviewStateService : IReviewStateService
    {
        private ReviewState _current;

        public void Initialize(int userId, string fileId)
        {
            _current = new(userId, fileId);
            State = Review.Initialized;
        }

        public int UserId => _current.UserId;
        public string FileId => _current.FileId;
        public Review State { get => _current.State; set => _current.State = value; }
    }

    public class ReviewState
    {
        public ReviewState(int userId, string fileId)
        {
            UserId = userId;
            FileId = fileId;
        }

        public int UserId { get; }
        public string FileId { get; }
        public Review State { get; set; }
    }
}
