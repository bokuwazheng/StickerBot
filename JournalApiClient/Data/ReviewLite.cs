using JournalApiClient.Data.Enums;
using Newtonsoft.Json;

namespace JournalApiClient.Data
{
    /// <summary>
    /// Lite version of JournalApiClient.Data.Review. Used as Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.CallbackData. 
    /// CallbackData is limited to 64 bytes per button.
    /// </summary>
    public class ReviewLite
    {
        public ReviewLite() { }

        public ReviewLite(int suggestionId, int userId) => (SuggestionId, SuggesterId) = (suggestionId, userId);

        [JsonProperty("Id")]
        public int SuggestionId { get; set; }

        [JsonProperty("By")]
        public int SuggesterId { get; set; }

        [JsonProperty("R")]
        public ReviewResult Result { get; set; }
    }
}
