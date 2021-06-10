namespace JournalApiClient.Data
{
    public class Sender // TODO: read-only properties must have 'init' instead of 'set'
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public bool IsBanned { get; set; }
        public long ChatId { get; set; }
        public bool Notify { get; set; }
    }
}
