namespace CodeMonkey.Core.Interfaces
{
    public interface IToolManager
    {
        string ExecuteTool(string name, string argsYaml, string workingDirectory, List<string>? permissions = null);
        Dictionary<string, string>? ParseArguments(string argsYaml);
    }
}
