using System.Text.Json.Serialization;

namespace CodeMonkey.Core.Models
{
    public class Choice
    {
        [JsonPropertyName("message")]
        public Message Message { get; set; }

        public override string ToString() => Message?.ToString() ?? "null";
    }
}
