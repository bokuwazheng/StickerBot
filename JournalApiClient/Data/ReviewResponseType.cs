using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace JournalApiClient.Data
{
    public class ReviewResponseType
    {
        [JsonProperty(NamingStrategyType = typeof(DefaultNamingStrategy))]
        public Review AddReview { get; set; }

        [JsonProperty(NamingStrategyType = typeof(DefaultNamingStrategy))]
        public Review NewReview { get; set; }

        public Review Review { get; set; }
    }
}
