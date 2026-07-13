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
        private readonly ITokenHelper _tokenHelper;
        private readonly JsonSerializerOptions _options;

        private const int _MAX_OUTPUT_LENGTH_TOKENS = 2500;

        public ToolManager(IFileSystem fileSystem, IShell shell, IUserPreferences userPreferences, ITokenHelper tokenHelper)
        {
            this._fileSystem = fileSystem;
            this._shell = shell;
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

        public ToolResult ExecuteTool(string name, string argsJson, string workingDirectory)
        {
            if (!IsToolSupported(name))
            {
                var unknownToolResult = $"Error: Tool {name} not found.";
                return ToolResult.Error(name, unknownToolResult, GetToolDescription(name));
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
                    "monkey_grep" => ExecuteGrep(argsJson, workingDirectory),
                    "file_exists" => ExecuteFileExists(argsJson, workingDirectory),
                    "write_file_range" => ExecuteWriteFileRange(argsJson, workingDirectory),
                    "get_file_list" => ExecuteGetFileList(argsJson, workingDirectory),
                    "run_command" => ExecuteRunCommand(argsJson, workingDirectory),
                    _ => throw new NotSupportedException($"Tool {name} is not supported.")
                };
            }
            catch (Exception ex)
            {
                return ToolResult.Error(name, ex, GetToolDescription(name));
            }

            var safeLengthResult = RestrictLength(executionResult);
            bool requiresRefresh = name == "write_file" || name == "write_file_range";
            return ToolResult.Success(name,
                safeLengthResult,
                GetToolDescription(name),
                requiresRefresh);
        }

        public static bool RequiredRefresh(string toolName) => toolName == "write_file" || toolName == "write_file_range";

        public string GetToolDescription(string name)
        {
            return name switch
            {
                "write_file" => "Writes content to a file",
                "read_file" => "Reads content from a file",
                "read_file_chunked" => "Reads a range of lines from a file",
                "read_file_head" => "Reads the first N lines of a file",
                "read_file_tail" => "Reads the last N lines of a file",
                "read_file_search" => "Searches for a term in a file",
                "monkey_grep" => "Searches for a regex pattern in a file",
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
                "monkey_grep" => true,
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

        public List<object> GetToolDefinitions(bool isSubagent = false)
        {
            var retVal = new List<object>
            {
                new {
                    type = "function",
                    function = new {
                        name = "monkey_grep",
                        description = "Searches for a regex pattern in a file. Custome replacement for normal 'grep' commands",
                        parameters = new {
                            type = "object",
                            properties = new {
                                pattern = new { type = "string", description = "The regex pattern to search for" },
                                path = new { type = "string", description = "The file path" }
                            },
                            required = new[] { "pattern", "path" }
                        }
                    }
                },
                new {
                    type = "function",
                    function = new {
                        name = "read_file_chunked",
                        description = "Reads a specific line range from a file. Line numbers are 1-indexed.",
                        parameters = new {
                            type = "object",
                            properties = new {
                                path = new { type = "string", description = "The file path" },
                                startLine = new { type = "integer", description = "The starting line number (1-indexed)" },
                                endLine = new { type = "integer", description = "The ending line number (1-indexed)" }
                            },
                            required = new[] { "path", "startLine", "endLine" }
                        }
                    }
                },
                new {
                    type = "function",
                    function = new {
                        name = "read_file_search",
                        description = "Searches for a term in a file and returns the matching lines with surrounding context. Line numbers are 1-indexed.",
                        parameters = new {
                            type = "object",
                            properties = new {
                                path = new { type = "string", description = "The file path" },
                                searchTerm = new { type = "string", description = "The string to search for" },
                                contextLines = new { type = "integer", description = "Number of context lines to provide around each match" }
                            },
                            required = new[] { "path", "searchTerm", "contextLines" }
                        }
                    }
                },
                new {
                    type = "function",
                    function = new {
                        name = "write_file_range",
                        description = "Performs a surgical update to a file. Line numbers are 1-indexed. Only one file write operation can be performed per turn.",
                        parameters = new {
                            type = "object",
                            properties = new {
                                path = new { type = "string", description = "The file path" },
                                startLine = new { type = "integer", description = "The starting line number of the range (1-indexed)" },
                                endLine = new { type = "integer", description = "The ending line number of the range (1-indexed)" },
                                content = new { type = "string", description = "The new content to place in the range" },
                                mode = new { type = "string", description = "The write mode: Replace, InsertBefore, InsertAfter, Delete" }
                            },
                            required = new[] { "path", "startLine", "endLine", "content", "mode" }
                        }
                    }
                },
                new {
                    type = "function",
                    function = new {
                        name = "get_file_list",
                        description = "Gets a list of accessible files in the directory as relative paths",
                        parameters = new {
                            type = "object",
                            properties = new {
                                recursive = new { type = "bool", description = "The file list will contain files in subdirectories" },
                                searchPattern = new { type = "string", description = "The search string to match against the names of files in path. " +
                                    "This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions" }
                            }
                        }
                    }
                },
                new {
                    type = "function",
                    function = new {
                        name = "run_command",
                        description = "Runs a shell command (e.g., 'dotnet build'). Do not use for file writes.",
                        parameters = new {
                            type = "object",
                            properties = new {
                                command = new { type = "string", description = "The shell command to execute" }
                            },
                            required = new[] { "command" }
                        }
                    }
                },
                new {
                    type = "function",
                    function = new {
                        name = "write_file",
                        description = "Writes content to a file at the specified path. Only one file write operation can be performed per turn.",
                        parameters = new {
                            type = "object",
                            properties = new {
                                path = new { type = "string", description = "The file path" },
                                content = new { type = "string", description = "The text content to write" }
                            },
                            required = new[] { "path", "content" }
                        }
                    }
                },
                new {
                    type = "function",
                    function = new {
                        name = "read_file",
                        description = "Reads the content of a file",
                        parameters = new {
                            type = "object",
                            properties = new {
                                path = new { type = "string", description = "The file path" }
                            },
                            required = new[] { "path" }
                        }
                    }
                },
                new {
                    type = "function",
                    function = new {
                        name = "read_file_head",
                        description = "Reads the first N lines of a file",
                        parameters = new {
                            type = "object",
                            properties = new {
                                path = new { type = "string", description = "The file path" },
                                lineCount = new { type = "integer", description = "Number of lines to read" }
                            },
                            required = new[] { "path", "lineCount" }
                        }
                    }
                },
                new {
                    type = "function",
                    function = new {
                        name = "read_file_tail",
                        description = "Reads the last N lines of a file",
                        parameters = new {
                            type = "object",
                            properties = new {
                                path = new { type = "string", description = "The file path" },
                                lineCount = new { type = "integer", description = "Number of lines to read" }
                            },
                            required = new[] { "path", "lineCount" }
                        }
                    }
                },
                new {
                    type = "function",
                    function = new {
                        name = "file_exists",
                        description = "Checks if a file exists",
                        parameters = new {
                            type = "object",
                            properties = new {
                                path = new { type = "string", description = "The file path" }
                            },
                            required = new[] { "path" }
                        }
                    }
                }
            };

            if (!isSubagent)
            {
                retVal.Add(new
                {
                    type = "function",
                    function = new
                    {
                        name = "dispatch_subagent",
                        description = "Use subagents for repetitive exploration, summarizing large volumes of data, or tasks that would generate excessive tool output. " +
                            "You can dispatch as many subagents as you want at once, they will execute sequentially so it will not impact performance.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                name = new { type = "string", description = "A short, human-readable name for the subagent" },
                                task = new { type = "string", description = "The specific objective for the subagent" },
                                permissions = new
                                {
                                    type = "array",
                                    items = new { type = "string" },
                                    description = "A list of allowed privileged tools (e.g., ['write_file', 'run_command'])"
                                },
                                initial_context = new
                                {
                                    type = "array",
                                    items = new { type = "string" },
                                    description = "A list of files the subagent should start with to minimize unnecessary tool calls"
                                }
                            },
                            required = new[] { "task" }
                        }
                    }
                }
                );
            }

            return retVal;
        }

    }
}
