using CodeMonkey.Core.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CodeMonkey.Core.Services
{

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

                var recursive = args.ContainsKey("recursive") ? args["recursive"] : "false";
                var searchPattern = args.ContainsKey("searchPattern") ? args["searchPattern"] : "*";

                return name switch
                {
                    "write_file" => _fileSystem.WriteFile(args["path"], args["content"], workingDirectory),
                    "read_file" => _fileSystem.ReadFile(args["path"], workingDirectory),
                    "get_file_list" => _fileSystem.GetFileList(recursive, searchPattern, workingDirectory),
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
