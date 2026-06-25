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

        public Dictionary<string, string>? ParseArguments(string argsYaml)
        {
            try
            {
                return _deserializer.Deserialize<Dictionary<string, string>>(argsYaml);
            }
            catch
            {
                return null;
            }
        }

        public T? ParseArguments<T>(string argsYaml)
        {
            try
            {
                return _deserializer.Deserialize<T>(argsYaml);
            }
            catch
            {
                return default;
            }
        }

        public string ExecuteTool(string name, string argsYaml, string workingDirectory, List<string>? permissions = null)
        {
            if (permissions != null)
            {
                if (IsPrivilegedTool(name) && !permissions.Contains(name))
                {
                    return $"Error: Subagent does not have permission to use tool '{name}'.";
                }
            }

            try
            {
                var args = ParseArguments(argsYaml);
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

        private bool IsPrivilegedTool(string name)
        {
            return name switch
            {
                "write_file" => true,
                "run_command" => true,
                _ => false
            };
        }
    }
}
