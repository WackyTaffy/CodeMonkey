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

        [JsonPropertyName("reasoning_content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [YamlMember(Alias = "reasoning_content")]
        public string? ReasoningContent { get; set; }

        [JsonPropertyName("tool_call_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [YamlMember(Alias = "tool_call_id")]
        public string? ToolCallId { get; set; }

        [JsonPropertyName("tool_calls")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [YamlMember(Alias = "tool_calls")]
        public List<ToolCall>? ToolCalls { get; set; }

        [SetsRequiredMembers]
        public Message(string role)
        {
            Role = role;
            Content = null;
            ToolCallId = null;
        }

        [SetsRequiredMembers]
        public Message(string role, string toolCallId)
        {
            Role = role;
            Content = null;
            ToolCallId = toolCallId;
        }

        public Message() { }

        public override string ToString() => $"[{Role}] {ToolCalls?.Count ?? 0} Tool Calls, Content Length = {Content?.Length ?? 0}, Reasoning Length = {ReasoningContent?.Length ?? 0}";

        public static Message WithToolCallList(string role, string toolCallId, List<ToolCall> toolCalls) => new Message(role, toolCallId) { ToolCalls = toolCalls };
        public static Message WithToolResult(string role, string toolCallId, ToolResult toolResult) => new Message(role, toolCallId) { Content = toolResult.Result };
        public static Message WithStringContent(string role, string toolCallId, string contentStr) => new Message(role, toolCallId) { Content = contentStr };
        public static Message WithStringContent(string role, string contentStr) => new Message(role) { Content = contentStr };
        public static Message WithToolResult(string role, ToolResult toolResult) => new Message(role) { Content = toolResult.Result };
        public static Message WithToolResultAndCallList(string role, string toolCallId, ToolResult toolResult, List<ToolCall> toolCalls) => new Message(role, toolCallId) { Content = toolResult.Result, ToolCalls = toolCalls };
        public static Message WithToolResultAndCallList(string role, ToolResult toolResult, List<ToolCall> toolCalls) => new Message(role) { Content = toolResult.Result, ToolCalls = toolCalls };

    }
}
