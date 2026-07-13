namespace CodeMonkey.Core.Interfaces
{
    public interface IToolManager
    {
        CodeMonkey.Core.Models.ToolResult ExecuteTool(string name, string argsJson, string workingDirectory, List<string>? permissions = null);
        Dictionary<string, string>? ParseArguments(string argsJson);
        T? ParseArguments<T>(string argsJson);
        List<object> GetToolDefinitions(bool isSubagent = false);
    }
}
