using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace JournalApiClient.Data
{
    public class ResponseSenderType
    {
        [JsonProperty(NamingStrategyType = typeof(DefaultNamingStrategy))]
        public Sender AddSender { get; set; }

        [JsonProperty(NamingStrategyType = typeof(DefaultNamingStrategy))]
        public Sender BanSender { get; set; }

        public Sender Sender { get; set; }
    }
}
