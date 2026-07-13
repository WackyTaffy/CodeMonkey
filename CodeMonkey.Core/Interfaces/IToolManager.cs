namespace CodeMonkey.Core.Interfaces
{
    public interface IToolManager
    {
        CodeMonkey.Core.Models.ToolResult ExecuteTool(string name, string argsJson, string workingDirectory);
        Dictionary<string, string>? ParseArguments(string argsJson);
        T? ParseArguments<T>(string argsJson);
        List<object> GetToolDefinitions(bool isSubagent = false);
    }
}
