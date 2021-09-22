namespace StickerBot.Options
{
    public class BotOptions
    {
        public int ChatId { get; set; }
        public string BotToken { get; set; }
        public string WebhookUrl { get; set; }
        public string BotLogin { get; set; }
        public string BotPassword { get; set; }
        /// <summary>
        /// Timeout in munites.
        /// </summary>
        public int ApiTimeout { get; set; } 
        public string ApiBaseAddress { get; set; }
        public string Guidelines { get; set; }
    }
}
