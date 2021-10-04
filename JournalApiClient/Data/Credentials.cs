using System.Text.Json.Serialization;

namespace JournalApiClient.Data
{
    public record Credentials
    {
        public Credentials() { }

        public Credentials(string login, string password) => (Login, Password) = (login, password);

        [JsonPropertyName("login")]
        public string Login { get; }

        [JsonPropertyName("password")]
        public string Password { get; }
    }
}
