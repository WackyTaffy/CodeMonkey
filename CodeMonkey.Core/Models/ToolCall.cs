using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace CodeMonkey.Core.Models
{
    public class ToolCall
    {
        [JsonPropertyName("id")]
        [YamlMember(Alias = "id")]
        public required string Id { get; set; }

        [JsonPropertyName("type")]
        [YamlMember(Alias = "type")]
        public string Type { get; set; } = "function";

        [JsonPropertyName("function")]
        [YamlMember(Alias = "function")]
        public required FunctionCall Function { get; set; }
    }
}
