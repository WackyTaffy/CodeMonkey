using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CodeMonkey.Core.Services
{
    public interface IToolManager
    {
        string ExecuteTool(string name, string argsYaml, string workingDirectory);
    }

    public class ToolManager : IToolManager
    {
        private readonly IFileSystem _fileSystem;
        private readonly IShell _shell;
        private readonly IDeserializer _deserializer;

        public ToolManager(IFileSystem fileSystem, IShell shell)
        {
            _fileSystem = fileSystem;
            _shell = shell;
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        public string ExecuteTool(string name, string argsYaml, string workingDirectory)
        {
            try
            {
                var args = _deserializer.Deserialize<Dictionary<string, string>>(argsYaml);
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
