using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
        public static Message AsUserMessage(string contentStr) => new Message("user") { Content = contentStr };
        public static Message AsAssistantMessage(string contentStr) => new Message("assistant") { Content = contentStr };
        public static Message AsAssistantMessage(string contentStr, string reasoningStr) => new Message("assistant") { Content = contentStr, ReasoningContent = reasoningStr };
        public static Message AsAssistantMessage(List<ToolCall> toolCalls) => new Message("assistant") { ToolCalls = toolCalls };
        public static Message AsAssistantMessage(string toolCallId, List<ToolCall> toolCalls) => new Message("assistant", toolCallId) { ToolCalls = toolCalls };
        public static Message AsAssistantMessage(string toolCallId, ToolResult toolResult, List<ToolCall> toolCalls) => new Message("assistant", toolCallId) { Content = toolResult.Result, ToolCalls = toolCalls };
        public static Message AsToolResult(string toolCallId, ToolResult toolResult) => new Message("tool", toolCallId) { Content = toolResult.Result };
        public static Message AsFileContents(string contentStr, string? fileName = null, bool truncated = false)
        {
            string? type =  GetType(fileName);

            string fileNameAttr = fileName == null ? "" : $" source=\"{fileName}\"";
            string truncatedAttr = truncated ? $" truncated=\"{truncated.ToString()}\"" : "";
            string typeAttr = type == null ? "" : $" type=\"{type}\"";

            return new Message("user") { Content = $"<context {fileNameAttr} {typeAttr} >{contentStr}</context>" };
        }

        internal static string? GetType(string? fileName)
        {
            if(string.IsNullOrWhiteSpace(fileName)) return null;

            string extension = Path.GetExtension(fileName);
            switch (extension)
            {
                case "cs":          return "csharp";
                case "md":          return "markdown";
                case "txt":         return "plain text";
                case "json":        return "json";
                case "csproj":      return "csharp project";
                case "gitignore":   return "git ignore";
                case "log":         return "log";

                case "sln":
                case "slnx":
                    return "csharp solution";

                default: return null;
            }
        }

        internal static string? GetCsharpType(string fileContents)
        {
            if (string.IsNullOrWhiteSpace(fileContents))
                return null;

            // Strip comments and string/char literals so keywords inside them
            // don't produce false positives.
            string cleaned = Regex.Replace(
                fileContents,
                @"//.*?$|/\*.*?\*/|""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])'",
                string.Empty,
                RegexOptions.Multiline | RegexOptions.Singleline);

            // Match a type-declaration keyword followed by an identifier.
            // Order in the alternation doesn't matter; we take the earliest match
            // in the source via Regex.Match, which scans left-to-right.
            var match = Regex.Match(
                cleaned,
                @"\b(class|struct|interface|enum|record)\b\s+[A-Za-z_]\w*");

            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
