using JournalApiClient.Json;
using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace JournalApiClient.Data.Enums
{
    [JsonConverter(typeof(CustomStringEnumConverter))]
    public enum ReviewResult
    {
        /// <summary>
        /// Skipped.
        /// </summary>
        [Description("No decision yet.")]
        None = 0,

        /// <summary>
        /// Most probably will be added to the sticker pack.
        /// </summary>
        [Description("Approved: Very nice!")]
        Approved = 1,

        /// <summary>
        /// Personal preferences or other reasons.
        /// </summary>
        [Description("Declined: Personal preferences or other reasons.")]
        Other = 20,

        /// <summary>
        /// Low quality.
        /// </summary>
        [Description("Declined: Low quality.")]
        LowQuality = 21,

        /// <summary>
        /// Does not fit.
        /// </summary>
        [Description("Declined: Does not fit.")]
        DoesNotFit = 22,

        /// <summary>
        /// Too similar to an existing sticker.
        /// </summary>
        [Description("Declined: Too similar to an existing sticker.")]
        TooSimilar = 23,

        /// <summary>
        /// Inappropriate content.
        /// </summary>
        [Description("Banned.")]
        Banned = 30
    }

    [Flags]
    [JsonConverter(typeof(CustomStringEnumConverter))]
    public enum ReviewFlag
    {
        /// <summary>
        /// Skipped.
        /// </summary>
        [Description("No decision yet.")]
        None = 1 << 0,

        /// <summary>
        /// Most probably will be added to the sticker pack.
        /// </summary>
        [Description("Approved.")]
        Approved = 1 << 1,

        /// <summary>
        /// Declined.
        /// </summary>
        [Description("Declined.")]
        Declined = 1 << 2,

        /// <summary>
        /// Personal preferences or other reasons.
        /// </summary>
        [Description("Declined: Personal preferences or other reasons.")]
        Other = Approved | Declined,

        /// <summary>
        /// Low quality.
        /// </summary>
        [Description("Declined: Low quality.")]
        LowQuality = 21,

        /// <summary>
        /// Does not fit.
        /// </summary>
        [Description("Declined: Does not fit.")]
        DoesNotFit = 22,

        /// <summary>
        /// Too similar to an existing sticker.
        /// </summary>
        [Description("Declined: Too similar to an existing sticker.")]
        TooSimilar = 23,

        /// <summary>
        /// Inappropriate content.
        /// </summary>
        [Description("Banned.")]
        Banned = 30
    }
}