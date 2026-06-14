using System.Text.Json.Serialization;

namespace CodeMonkey.Core.Models
{
    public class ToolCall
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        [JsonPropertyName("function")]
        public FunctionCall Function { get; set; }
    }
}
