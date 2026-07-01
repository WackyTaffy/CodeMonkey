using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using CodeMonkey.Core.Utility;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CodeMonkey.Core.Services
{
    public class ToolManager : IToolManager
    {
        private readonly IFileSystem _fileSystem;
        private readonly IShell _shell;
        private readonly IManifestService _manifestService;
        private readonly IUserPreferences _userPreferences;
        private readonly ISessionLedger _sessionLedger;
        private readonly ITokenHelper _tokenHelper;
        private readonly JsonSerializerOptions _options;

        private const int _MAX_OUTPUT_LENGTH_TOKENS = 2500;

        public ToolManager(IFileSystem fileSystem, IShell shell, IManifestService manifestService, IUserPreferences userPreferences, ISessionLedger sessionLedger, ITokenHelper tokenHelper)
        {
            this._fileSystem = fileSystem;
            this._shell = shell;
            this._manifestService = manifestService;
            this._userPreferences = userPreferences;
            this._sessionLedger = sessionLedger;
            this._tokenHelper = tokenHelper;
            this._options = new JsonSerializerOptions
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

        public void ApproveManifest(Guid id)
        {
            _manifestService.ApproveManifest(id);
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

            if (!IsToolSupported(name))
            {
                var unknownToolResult = $"Error: Tool {name} not found.";
                _sessionLedger.RecordAction(name, false, $"Args: {argsJson} | Result: {unknownToolResult}");
                return unknownToolResult;
            }

            string executionResult;
            bool success;
            try
            {
                executionResult = name switch
                {
                    "write_file" => ExecuteWriteFile(argsJson, workingDirectory),
                    "read_file" => ExecuteReadFile(argsJson, workingDirectory),
                    "read_file_chunked" => ExecuteReadFileChunked(argsJson, workingDirectory),
                    "get_file_list" => ExecuteGetFileList(argsJson, workingDirectory),
                    "run_command" => ExecuteRunCommand(argsJson, workingDirectory),
                    _ => $"Error: Tool {name} not found."
                };
                success = !executionResult.StartsWith("Error:");
            }
            catch (Exception Exception)
            {
                executionResult = $"Error executing tool {name}: {Exception.Message}";
                success = false;
            }

            _sessionLedger.RecordAction(name, success, $"Args: {argsJson} | Result: {executionResult}");

            var safeLengthResult = RestrictLength(executionResult);
            return safeLengthResult;
        }

        private string RestrictLength(string str)
        {
            var tokenLength = _tokenHelper.GetTokenCount(str);
            if (tokenLength < _MAX_OUTPUT_LENGTH_TOKENS)
                return str;

            var strBuilder = new StringBuilder();

            Func<string, bool> willHitTokenCap = (string appendStr) =>
            {
                int currentTokenCount = _tokenHelper.GetTokenCount(strBuilder.ToString());
                int lineTokenCount = _tokenHelper.GetTokenCount(appendStr);
                return (currentTokenCount + lineTokenCount) >= _MAX_OUTPUT_LENGTH_TOKENS;
            };

            using (StringReader reader = new StringReader(str))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if(willHitTokenCap(line))
                    {
                        strBuilder.AppendLine("WARNING! Results truncated due to excessive length");
                        break;
                    }

                    strBuilder.AppendLine(line);
                }
            }

            return strBuilder.ToString();
        }

        private bool IsToolSupported(string name)
        {
            return name switch
            {
                "write_file" => true,
                "read_file" => true,
                "read_file_chunked" => true,
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
                "run_command" => true,
                _ => false
            };
        }
    }
}
