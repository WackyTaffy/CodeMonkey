using Google.Protobuf;
using System.Linq;

namespace CodeMonkey.Core.Models;

public class ToolResult
{
    public string Result { get; set; }
    public string ToolName { get; set; }
    public string Description { get; set; }
    public bool IsSuccess { get; set; }
    public bool RequiresContextRefresh { get; set; }

    private ToolResult(string toolName, bool success, string? result = null, string? description = null, bool requiresContextRefresh = false)
    {
        ToolName = toolName;
        IsSuccess = success;
        Result = result ?? string.Empty;
        Description = description ?? string.Empty;
        RequiresContextRefresh = requiresContextRefresh;
    }

    public static ToolResult Error(string toolName, string message) => new(toolName, false, message);
    public static ToolResult Error(string toolName, string message, string description) => new(toolName, false, message, description);
    public static ToolResult Error(string toolName, Exception ex) => new(toolName, false, ex.Message);
    public static ToolResult Error(string toolName, Exception ex, string description) => new(toolName, false, ex.Message, description);
    public static ToolResult Success(string toolName) => new(toolName,  true);
    public static ToolResult Success(string toolName, string result) => new(toolName,  true, result);
    public static ToolResult Success(string toolName, string result, string description) => new(toolName, true, result, description);
    public static ToolResult Success(string toolName, string result, string description, bool requiresContextRefresh) => new(toolName, true, result, description, requiresContextRefresh);

    public override string ToString() => $"[{ToolName}] {(IsSuccess ? "SUCCESS" : "ERROR")} - {Result}";
    public string ToStringShort()
    {
        string resultStr = IsSuccess
            ? $"Result Length = {Result.Length} characters / {Result.Count(x => x.Equals(Environment.NewLine))} lines"
            : $"Result = {Result}";

        return $"[{ToolName}] {(IsSuccess ? "SUCCESS" : "ERROR")}, {resultStr}";
    }
}
