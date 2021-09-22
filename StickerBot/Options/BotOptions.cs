namespace StickerBot.Options
{
    public class BotOptions
    {
        /// <summary>
        /// Reviewer chat ID.
        /// </summary>
        public int ChatId { get; set; }

        /// <summary>
        /// Bot token acquired from BotFather.
        /// </summary>
        public string BotToken { get; set; }

        /// <summary>
        /// Webhook URL.
        /// </summary>
        public string WebhookUrl { get; set; }

        /// <summary>
        /// Login used in bot authentication.
        /// </summary>
        public string BotLogin { get; set; }

        /// <summary>
        /// Password used in bot authentication.
        /// </summary>
        public string BotPassword { get; set; }

        /// <summary>
        /// Timeout in minutes.
        /// </summary>
        public int ApiTimeout { get; set; } 

        /// <summary>
        /// Backend URI.
        /// </summary>
        public string ApiBaseAddress { get; set; }

        /// <summary>
        /// Guidelines URI.
        /// </summary>
        public string Guidelines { get; set; }
    }
}
