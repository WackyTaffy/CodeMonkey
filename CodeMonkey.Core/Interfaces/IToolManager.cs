namespace CodeMonkey.Core.Interfaces
{
    public interface IToolManager
    {
        string ExecuteTool(string name, string argsYaml, string workingDirectory);
    }
}
