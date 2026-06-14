using System.Text.Json.Serialization;

namespace CodeMonkey.Core.Models
{
    public class ChatResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; }

        public override string ToString() => string.Join("\n", Choices ?? []);
    }
}
