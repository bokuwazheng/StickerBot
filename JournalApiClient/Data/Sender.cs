namespace JournalApiClient.Data
{
    public record Sender
    {
        public int UserId { get; init; }
        public string FirstName { get; init; }
        public string LastName { get; init; }
        public string Username { get; init; }
        public long ChatId { get; init; }
        public bool IsBanned { get; init; }
        public bool Notify { get; init; }
    }
}
