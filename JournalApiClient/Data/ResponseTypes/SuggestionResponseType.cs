using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace JournalApiClient.Data.ResponseTypes
{
    public class SuggestionResponseType
    {
        [JsonProperty(NamingStrategyType = typeof(DefaultNamingStrategy))]
        public Suggestion AddSuggestion { get; set; }

        [JsonProperty(NamingStrategyType = typeof(DefaultNamingStrategy))]
        public Suggestion NewSuggestion { get; set; }

        public Suggestion Suggestion { get; set; }
    }
}
