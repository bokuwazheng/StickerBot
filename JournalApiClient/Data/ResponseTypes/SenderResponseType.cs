using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace JournalApiClient.Data.ResponseTypes
{
    public class SenderResponseType
    {
        [JsonProperty(NamingStrategyType = typeof(DefaultNamingStrategy))]
        public Sender AddSender { get; set; }

        public Sender Sender { get; set; }

        public Sender Suggester { get; set; }

        [JsonProperty(NamingStrategyType = typeof(DefaultNamingStrategy))]
        public Sender UpdateSender { get; set; }
    }
}
