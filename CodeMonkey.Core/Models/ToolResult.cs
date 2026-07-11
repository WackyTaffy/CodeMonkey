using Google.Protobuf;

namespace CodeMonkey.Core.Models;

public class ToolResult
{
    public string Result { get; set; }
    public string ToolName { get; set; }
    public string Description { get; set; }
    public bool IsSuccess { get; set; }

    private ToolResult(string toolName, bool success, string? result = null, string? description = null)
    {
        ToolName = toolName;
        IsSuccess = success;
        Result = result ?? string.Empty;
        Description = description ?? string.Empty;
    }

    public static ToolResult Error(string toolName, string message) => new(toolName, false, message);
    public static ToolResult Error(string toolName, string message, string description) => new(toolName, false, message, description);
    public static ToolResult Error(string toolName, Exception ex) => new(toolName, false, ex.Message);
    public static ToolResult Error(string toolName, Exception ex, string description) => new(toolName, false,ex.Message, description);
    public static ToolResult Success(string toolName) => new(toolName,  true);
    public static ToolResult Success(string toolName, string result) => new(toolName,  true, result);
    public static ToolResult Success(string toolName, string result, string description) => new(toolName, true, result, description);

    public override string ToString() => $"[{ToolName}] {(IsSuccess ? "SUCCESS" : "ERROR")} - {Result}";
    public string ToStringShort() => $"[{ToolName}] {(IsSuccess ? "SUCCESS" : "ERROR")}, Result Length = {Result.Length} characters / {Result.Count(x => x.Equals(Environment.NewLine))} lines";
}
