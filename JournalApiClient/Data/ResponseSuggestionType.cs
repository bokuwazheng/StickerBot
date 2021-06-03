using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace JournalApiClient.Data
{
    public class ResponseSuggestionType
    {
        [JsonProperty(NamingStrategyType = typeof(DefaultNamingStrategy))]
        public Suggestion AddSuggestion { get; set; }

        public Suggestion Suggestion { get; set; }
    }
}
