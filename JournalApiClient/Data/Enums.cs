namespace JournalApiClient.Data.Enums
{
    public enum Status
    {
        Approved,
        Declined,
        New,
        UnderConsideration,
    }

    public enum Review
    {
        Initialized,
        Status,
        Comment,
        Notify,
        Finished
    }
}