namespace CodeMonkey.Core.Models;

public class ToolResult
{
    public string Result { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Success { get; set; }
}
