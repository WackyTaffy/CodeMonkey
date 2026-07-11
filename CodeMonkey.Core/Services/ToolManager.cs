using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeMonkey.Core.Services
{
    public class ToolManager : IToolManager
    {
        private readonly IFileSystem _fileSystem;
        private readonly IShell _shell;
        private readonly IUserPreferences _userPreferences;
        private readonly ITokenHelper _tokenHelper;
        private readonly JsonSerializerOptions _options;

        private const int _MAX_OUTPUT_LENGTH_TOKENS = 2500;

        public ToolManager(IFileSystem fileSystem, IShell shell, IUserPreferences userPreferences, ITokenHelper tokenHelper)
        {
            this._fileSystem = fileSystem;
            this._shell = shell;
            this._userPreferences = userPreferences;
            this._tokenHelper = tokenHelper;
            this._options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
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

        public ToolResult ExecuteTool(string name, string argsJson, string workingDirectory, List<string>? permissions = null)
        {
            if (permissions != null && permissions.Any())
            {
                if (IsPrivilegedTool(name) && !permissions.Contains(name))
                {
                    return ToolResult.Error(name, 
                        $"Error: Subagent does not have permission to use tool '{name}'.", 
                        GetToolDescription(name, argsJson));
                }
            }

            if (!IsToolSupported(name))
            {
                var unknownToolResult = $"Error: Tool {name} not found.";
                return ToolResult.Error(name, unknownToolResult, GetToolDescription(name, argsJson));
            }

            string executionResult;
            try
            {
                executionResult = name switch
                {
                    "write_file" => ExecuteWriteFile(argsJson, workingDirectory),
                    "read_file" => ExecuteReadFile(argsJson, workingDirectory),
                    "read_file_chunked" => ExecuteReadFileChunked(argsJson, workingDirectory),
                    "read_file_search" => ExecuteReadFileSearch(argsJson, workingDirectory),
                    "read_file_head" => ExecuteReadFileHead(argsJson, workingDirectory),
                    "read_file_tail" => ExecuteReadFileTail(argsJson, workingDirectory),
                    "grep" => ExecuteGrep(argsJson, workingDirectory),
                    "file_exists" => ExecuteFileExists(argsJson, workingDirectory),
                    "write_file_range" => ExecuteWriteFileRange(argsJson, workingDirectory),
                    "get_file_list" => ExecuteGetFileList(argsJson, workingDirectory),
                    "run_command" => ExecuteRunCommand(argsJson, workingDirectory),
                    _ => throw new NotSupportedException($"Tool {name} is not supported.")
                };
            }
            catch (Exception ex)
            {
                return ToolResult.Error(name, ex, GetToolDescription(name, argsJson));
            }

            var safeLengthResult = RestrictLength(executionResult);
            return ToolResult.Success(name,
                safeLengthResult,
                GetToolDescription(name, argsJson));
        }

        private string GetToolDescription(string name, string argsJson)
        {
            return name switch
            {
                "write_file" => "Writes content to a file",
                "read_file" => "Reads content from a file",
                "read_file_chunked" => "Reads a range of lines from a file",
                "read_file_head" => "Reads the first N lines of a file",
                "read_file_tail" => "Reads the last N lines of a file",
                "grep" => "Searches for a regex pattern in a file",
                "file_exists" => "Checks if a file exists",
                "write_file_range" => "Performs a surgical update to a file range",
                "get_file_list" => "Lists files in a directory",
                "run_command" => "Runs a shell command",
                _ => "No description available"
            };
        }

        private string RestrictLength(string str)
        {
            var tokenLength = _tokenHelper.GetTokenCount(str);
            if (tokenLength < _MAX_OUTPUT_LENGTH_TOKENS)
                return str;

            return str.Substring(0, (int)(str.Length * 0.8)) + "... [Truncated]";
        }

        private bool IsToolSupported(string name)
        {
            return name switch
            {
                "write_file" => true,
                "read_file" => true,
                "read_file_chunked" => true,
                "read_file_search" => true,
                "read_file_head" => true,
                "read_file_tail" => true,
                "grep" => true,
                "file_exists" => true,
                "write_file_range" => true,
                "get_file_list" => true,
                "run_command" => true,
                _ => false
            };
        }

        private string ExecuteWriteFile(string argsJson, string workingDirectory)
        {
            var args = ParseArguments<WriteFileArgs>(argsJson);
            if (args == null) throw new ArgumentException("Invalid arguments");
            return _fileSystem.WriteFile(args.Path, args.Content, workingDirectory);
        }

        private string ExecuteReadFile(string argsJson, string workingDirectory)
        {
            var args = ParseArguments<ReadFileArgs>(argsJson);
            if (args == null) throw new ArgumentException("Invalid arguments");
            return _fileSystem.ReadFile(args.Path, workingDirectory);
        }

        private string ExecuteReadFileChunked(string argsJson, string workingDirectory)
        {
            var args = ParseArguments<ReadFileChunkedArgs>(argsJson);
            if (args == null) throw new ArgumentException("Invalid arguments");
            return _fileSystem.ReadFileRange(args.Path, args.StartLine, args.EndLine, workingDirectory);
        }

        private string ExecuteReadFileSearch(string argsJson, string workingDirectory)
        {
            var args = ParseArguments<ReadFileWithSearchArgs>(argsJson);
            if (args == null) throw new ArgumentException("Invalid arguments");
            return _fileSystem.ReadFileWithSearch(args.Path, args.SearchTerm, args.ContextLines, workingDirectory);
        }

        private string ExecuteReadFileHead(string argsJson, string workingDirectory)
        {
            var args = ParseArguments<ReadFileHeadArgs>(argsJson);
            if (args == null) throw new ArgumentException("Invalid arguments");
            return _fileSystem.ReadFileHead(args.Path, args.LineCount, workingDirectory);
        }

        private string ExecuteReadFileTail(string argsJson, string workingDirectory)
        {
            var args = ParseArguments<ReadFileTailArgs>(argsJson);
            if (args == null) throw new ArgumentException("Invalid arguments");
            return _fileSystem.ReadFileTail(args.Path, args.LineCount, workingDirectory);
        }

        private string ExecuteGrep(string argsJson, string workingDirectory)
        {
            var args = ParseArguments<GrepArgs>(argsJson);
            if (args == null) throw new ArgumentException("Invalid arguments");
            return _fileSystem.Grep(args.Pattern, args.Path, workingDirectory);
        }

        private string ExecuteFileExists(string argsJson, string workingDirectory)
        {
            var args = ParseArguments<FileExistsArgs>(argsJson);
            if (args == null) throw new ArgumentException("Invalid arguments");
            return _fileSystem.FileExists(args.Path, workingDirectory) ? "True" : "False";
        }

        private string ExecuteWriteFileRange(string argsJson, string workingDirectory)
        {
            var args = ParseArguments<WriteFileRangeArgs>(argsJson);
            if (args == null) throw new ArgumentException("Invalid arguments");
            _fileSystem.WriteFileRange(args.Path, args.StartLine, args.EndLine, args.Content, args.Mode, workingDirectory);
            return $"Successfully updated {args.Path} in range {args.StartLine}-{args.EndLine} using mode {args.Mode}.";
        }

        private string ExecuteGetFileList(string argsJson, string workingDirectory)
        {
            var args = ParseArguments<GetFileListArgs>(argsJson);
            if (args == null) throw new ArgumentException("Invalid arguments");
            return _fileSystem.GetFileList(args.Recursive, args.SearchPattern, workingDirectory);
        }

        private string ExecuteRunCommand(string argsJson, string workingDirectory)
        {
            var args = ParseArguments<RunCommandArgs>(argsJson);
            if (args == null) throw new ArgumentException("Invalid arguments");
            return _shell.RunCommand(args.Command, workingDirectory);
        }

        private bool IsPrivilegedTool(string name)
        {
            return name switch
            {
                "write_file" => true,
                "write_file_range" => true,
                "run_command" => true,
                _ => false
            };
        }
    }
}
