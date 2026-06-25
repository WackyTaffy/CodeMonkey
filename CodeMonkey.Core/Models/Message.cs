using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Serialization;

namespace CodeMonkey.Core.Models
{
    public class Message
    {
        [JsonPropertyName("role")]
        [YamlMember(Alias = "role")]
        public required string Role { get; set; }

        [JsonPropertyName("content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [YamlMember(Alias = "content")]
        public string? Content { get; set; }

        [JsonPropertyName("tool_call_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [YamlMember(Alias = "tool_call_id")]
        public string? ToolCallId { get; set; }

        [JsonPropertyName("tool_calls")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [YamlMember(Alias = "tool_calls")]
        public List<ToolCall>? ToolCalls { get; set; }

        [SetsRequiredMembers]
        public Message(string role, string content, string? toolCallId = null)
        {
            Role = role;
            Content = content;
            ToolCallId = toolCallId;
        }

        public Message() { }

        public override string ToString() => $"[{Role}] {ToolCalls?.Count ?? 0} Tool Calls, Content Length = {Content?.Length ?? 0}";
    }
}
