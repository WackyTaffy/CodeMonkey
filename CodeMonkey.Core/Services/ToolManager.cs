using CodeMonkey.Core.Interfaces;
using System.Text.Json;

namespace CodeMonkey.Core.Services
{
    public class ToolManager : IToolManager
    {
        private readonly IFileSystem _fileSystem;
        private readonly IShell _shell;
        private readonly JsonSerializerOptions _options;

        public ToolManager(IFileSystem fileSystem, IShell shell)
        {
            _fileSystem = fileSystem;
            _shell = shell;
            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public Dictionary<string, string>? ParseArguments(string argsJson)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(argsJson, _options);
            }
            catch
            {
                return null;
            }
        }

        public T? ParseArguments<T>(string argsJson)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(argsJson, _options);
            }
            catch
            {
                return default;
            }
        }

        public string ExecuteTool(string name, string argsJson, string workingDirectory, List<string>? permissions = null)
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
                var args = ParseArguments(argsJson);
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
