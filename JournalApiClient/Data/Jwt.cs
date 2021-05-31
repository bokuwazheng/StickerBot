using System.Text.Json.Serialization;

namespace JournalApiClient.Data
{
    public class Jwt
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}
