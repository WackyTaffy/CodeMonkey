using System.Text.Json.Serialization;

namespace CodeMonkey.Core.Models
{
    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Content { get; set; }

        [JsonPropertyName("tool_call_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ToolCallId { get; set; }

        [JsonPropertyName("tool_calls")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ToolCall> ToolCalls { get; set; }

        public Message(string role, string content, string toolCallId = null)
        {
            Role = role;
            Content = content;
            ToolCallId = toolCallId;
        }

        public Message() { }

        public override string ToString() => $"[{Role}] {ToolCalls?.Count ?? 0} Tool Calls, Content Length = {Content?.Length ?? 0}";
    }
}
