using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
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
        private Message(string role)
        {
            Role = role;
            Content = null;
            ToolCallId = null;
        }

        [SetsRequiredMembers]
        private Message(string role, string toolCallId)
        {
            Role = role;
            Content = null;
            ToolCallId = toolCallId;
        }

        public Message() { }

        public override string ToString() => $"[{Role}] {ToolCalls?.Count ?? 0} Tool Calls, Content Length = {Content?.Length ?? 0}, Reasoning Length = {ReasoningContent?.Length ?? 0}";

        public static Message AsSystemPrompt(string contentStr) => new Message("system") { Content = contentStr };
        public static Message AsContext(string contentStr) => new Message("context") { Content = contentStr };
        public static Message AsUserMessage(string contentStr) => new Message("user") { Content = contentStr };
        public static Message AsAssistantMessage(string contentStr) => new Message("assistant") { Content = contentStr };
        public static Message AsAssistantMessage(string contentStr, string reasoningStr) => new Message("assistant") { Content = contentStr, ReasoningContent = reasoningStr };
        public static Message AsAssistantMessage(List<ToolCall> toolCalls) => new Message("assistant") { ToolCalls = toolCalls };
        public static Message AsAssistantMessage(string toolCallId, List<ToolCall> toolCalls) => new Message("assistant", toolCallId) { ToolCalls = toolCalls };
        public static Message AsAssistantMessage(string toolCallId, ToolResult toolResult, List<ToolCall> toolCalls) => new Message("assistant", toolCallId) { Content = toolResult.Result, ToolCalls = toolCalls };
        public static Message AsToolResult(string toolCallId, ToolResult toolResult) => new Message("tool", toolCallId) { Content = toolResult.Result };
    }
}
