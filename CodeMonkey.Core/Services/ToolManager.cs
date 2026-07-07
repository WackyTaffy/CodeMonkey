using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using CodeMonkey.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.IO;

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
            if (permissions != null)
            {
                if (IsPrivilegedTool(name) && !permissions.Contains(name))
                {
                    return new ToolResult { 
                        Result = $"Error: Subagent does not have permission to use tool '{name}'.",
                        ToolName = name,
                        Description = GetToolDescription(name, argsJson),
                        Success = false
                    };
                }
            }

            if (!IsToolSupported(name))
            {
                var unknownToolResult = $"Error: Tool {name} not found.";
                _sessionLedger.RecordAction(name, false, $"Args: {argsJson} | Result: {unknownToolResult}");
                return new ToolResult { 
                    Result = unknownToolResult, 
                    ToolName = name, 
                    Description = GetToolDescription(name, argsJson), 
                    Success = false 
                };
            }

            // Confidence Gating Logic
            var risk = GetRiskLevel(name);
            var actionName = name == "run_command" ? "Shell: run_command" : name;
            var description = GetToolDescription(name, argsJson);
            
            var manifest = _manifestService.CreateManifest(actionName, risk, description, argsJson);
            
            //if (manifest == null || !_manifestService.RequestApproval(manifest, _userPreferences.ActiveProfile))
            //{
            //    var manifestId = manifest?.Id.ToString() ?? "N/A";
            //    var resultText = $"Action '{actionName}' requires manual approval. Manifest ID: {manifestId}";
            //    return new ToolResult { 
            //        Result = resultText, 
            //        ToolName = name, 
            //        Description = description, 
            //        Success = false 
            //    };
            //}

            string executionResult;
            bool success;
            try
            {
                executionResult = name switch
                {
                    "write_file" => ExecuteWriteFile(argsJson, workingDirectory),
                    "read_file" => ExecuteReadFile(argsJson, workingDirectory),
                    "read_file_chunked" => ExecuteReadFileChunked(argsJson, workingDirectory),
                    "read_file_search" => ExecuteReadFileSearch(argsJson, workingDirectory),
                    "write_file_range" => ExecuteWriteFileRange(argsJson, workingDirectory),
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
            return ToolResult.Success(safeLengthResult);
        }

        private (RiskLevel Risk, string Description, string[] Args) GetManifestDetails(string name, string argsJson)
        {
            switch (name)
            {
                case "write_file":
                    var writeArgs = ParseArguments<WriteFileArgs>(argsJson);
                    return (RiskLevel.Medium, $"Write to file: {writeArgs?.Path}", new[] { writeArgs?.Path ?? "unknown" });
                case "read_file":
                    var readArgs = ParseArguments<ReadFileArgs>(argsJson);
                    return (RiskLevel.Low, $"Read file: {readArgs?.Path}", new[] { readArgs?.Path ?? "unknown" });
                case "read_file_chunked":
                    var chunkArgs = ParseArguments<ReadFileChunkedArgs>(argsJson);
                    return (RiskLevel.Low, $"Read chunk of file: {chunkArgs?.Path}", new[] { chunkArgs?.Path ?? "unknown" });
                case "get_file_list":
                    var listArgs = ParseArguments<GetFileListArgs>(argsJson);
                    return (RiskLevel.Low, $"List files with pattern: {listArgs?.SearchPattern}", new[] { listArgs?.SearchPattern ?? "unknown" });
                case "run_command":
                    var cmdArgs = ParseArguments<RunCommandArgs>(argsJson);
                    return (RiskLevel.High, $"Run command: {cmdArgs?.Command}", new[] { cmdArgs?.Command ?? "unknown" });
                default:
                    return (RiskLevel.Low, "Unknown tool", new string[0]);
            }
        }

        private string RestrictLength(string str)
        {
            var tokenLength = _tokenHelper.GetTokenCount(str);
            if (tokenLength < _MAX_OUTPUT_LENGTH_TOKENS)
                return str;

            var strBuilder = new StringBuilder();

            return new ToolResult
            {
                Result = executionResult,
                ToolName = name,
                Description = description,
                Success = success
            };
        }

        private bool IsToolSupported(string name)
        {
            return name switch
            {
                "write_file" => true,
                "read_file" => true,
                "read_file_chunked" => true,
                "read_file_search" => true,
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
            if (args == null) throw new ArgumentException("InvalidArguments");
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
