using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace CodeMonkey.Core.Models
{
    public class Choice
    {
        [JsonPropertyName("message")]
        [YamlMember(Alias = "message")]
        public Message Message { get; set; }

        public override string ToString() => Message?.ToString() ?? "null";
    }
}
