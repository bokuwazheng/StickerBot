using System.ComponentModel;

namespace JournalApiClient.Data.Enums
{
    public enum ReviewResult : byte
    {
        /// <summary>
        /// Skipped.
        /// </summary>
        [Description("No result.")]
        None = 0,

        /// <summary>
        /// Most probably will be added to the sticker pack.
        /// </summary>
        [Description("Very nice!")]
        Approved = 1,

        /// <summary>
        /// Personal preferences or other reasons.
        /// </summary>
        [Description("Personal preferences or other reasons.")]
        Other = 20,

        /// <summary>
        /// Low quality.
        /// </summary>
        [Description("Low quality.")]
        LowQuality = 21,

        /// <summary>
        /// Does not fit.
        /// </summary>
        [Description("Does not fit.")]
        DoesNotFit = 22,

        /// <summary>
        /// Too similar to an existing sticker.
        /// </summary>
        [Description("Too similar to an existing sticker.")]
        TooSimilar = 23,

        /// <summary>
        /// Inappropriate content.
        /// </summary>
        [Description("Banned.")]
        Banned = 30
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