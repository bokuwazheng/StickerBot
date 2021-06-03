namespace JournalApiClient.Data.Enums
{
    public enum Status
    {
        /// <summary>
        /// In queue for review.
        /// </summary>
        New,

        /// <summary>
        /// Sent for review.
        /// </summary>
        Review,

        /// <summary>
        /// Most probably will be added to the sticker pack.
        /// </summary>
        Approved,

        /// <summary>
        /// Does not fit.
        /// </summary>
        Declined,

        /// <summary>
        /// Inappropriate content. Unlike other values, does not represent corresponding PostgreSQL enum value.
        /// </summary>
        Banned
    }

    public enum Stage
    {
        Initialized,
        Status,
        Comment,
        Notify,
        Finished
    }
}