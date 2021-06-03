using JournalApiClient.Data.Enums;

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
        public int UserId { get; set; }
        public string FileId { get; set; }
        public Status? Status { get; set; }
        public string Comment { get; set; }
        public bool Notify { get; set; }
    }
}
