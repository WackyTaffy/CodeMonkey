using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System.Text.Json;

namespace CodeMonkey.Core.Services
{
    public interface IToolManager
    {
        string ExecuteTool(string name, string argsJson, string workingDirectory);
    }

    public class ToolManager : IToolManager
    {
        private readonly IFileSystem _fileSystem;
        private readonly IShell _shell;

        public ToolManager(IFileSystem fileSystem, IShell shell)
        {
            _fileSystem = fileSystem;
            _shell = shell;
        }

        public string ExecuteTool(string name, string argsJson, string workingDirectory)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, string>>(argsJson);
                if (args == null) return "Error: Invalid arguments";

                return name switch
                {
                    "write_file" => _fileSystem.WriteFile(args["path"], args["content"], workingDirectory),
                    "read_file" => _fileSystem.ReadFile(args["path"], workingDirectory),
                    "run_command" => _shell.RunCommand(args["command"], workingDirectory),
                    _ => $"Error: Tool {name} not found."
                };
            }
            catch (Exception ex)
            {
                return $"Error executing tool {name}: {ex.Message}";
            }
        }
    }
}
