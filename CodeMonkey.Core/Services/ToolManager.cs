using CodeMonkey.Core.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeMonkey.Core.Models;
using System;
using System.Collections.Generic;

namespace CodeMonkey.Core.Services
{
    public class ToolManager : IToolManager
    {
        private readonly IFileSystem _fileSystem;
        private readonly IShell _shell;
        private readonly IManifestService _manifestService;
        private readonly IUserPreferences _userPreferences;
        private readonly ISessionLedger _sessionLedger;
        private readonly JsonSerializerOptions _options;

        public ToolManager(IFileSystem fileSystem, IShell shell, IManifestService manifestService, IUserPreferences userPreferences, ISessionLedger sessionLedger)
        {
            this._fileSystem = fileSystem;
            this._shell = shell;
            this._manifestService = manifestService;
            this._userPreferences = userPreferences;
            this._sessionLedger = sessionLedger;
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

            // Confidence Gating Logic
            var risk = GetRiskLevel(name);
            var actionName = name == "run_command" ? "Shell: run_command" : name;
            var description = GetToolDescription(name, argsJson);
            
            var manifest = _manifestService.CreateManifest(actionName, risk, description, argsJson);
            
            if (manifest == null || !_manifestService.RequestApproval(manifest, _userPreferences.ActiveProfile))
            {
                var manifestId = manifest?.Id.ToString() ?? "N/A";
                return $"Action '{actionName}' requires manual approval. Manifest ID: {manifestId}";
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
                    "read_file_search" => ExecuteReadFileSearch(argsJson, workingDirectory),
                    "write_file_range" => ExecuteWriteFileRange(argsJson, workingDirectory),
                    "get_file_list" => ExecuteGetFileList(argsJson, workingDirectory),
                    "run_command" => ExecuteRunCommand(argsJson, workingDirectory),
                    _ => $"Error: Tool {name} not found." // Should not be reached due to IsToolSupported check
                };
                success = !executionResult.StartsWith("Error:");
            }
            catch (Exception Exception)
            {
                executionResult = $"Error executing tool {name}: {Exception.Message}";
                success = false;
            }

            _sessionLedger.RecordAction(actionName, success, $"Args: {argsJson} | Result: {executionResult}");

            return executionResult;
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

        private RiskLevel GetRiskLevel(string name)
        {
            return name switch
            {
                "read_file" => RiskLevel.Low,
                "read_file_chunked" => RiskLevel.Low,
                "read_file_search" => RiskLevel.Low,
                "get_file_list" => RiskLevel.Low,
                "write_file" => RiskLevel.Medium,
                "write_file_range" => RiskLevel.Medium,
                "run_command" => RiskLevel.High,
                _ => RiskLevel.High
            };
        }

        private string GetToolDescription(string name, string argsJson)
        {
            return name switch
            {
                "read_file_search" => $"Searching for content in file. Results will include surrounding context. IMPORTANT: Line numbers are 1-indexed. Args: {argsJson}",
                "write_file_range" => $"Performing surgical update to a file. IMPORTANT: Line numbers are 1-indexed. Args: {argsJson}",
                _ => $"Executing tool {name} with arguments {argsJson};"
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
            return _fileSystem.ReadFileChunked(args.Path, args.StartLine, args.EndLine, workingDirectory);
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
