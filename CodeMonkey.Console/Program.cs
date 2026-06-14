using CodeMonkey.Core.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using YamlDotNet;

namespace CodeMonkey.Cli
{
    public class Program
    {
        public static string WorkingDirectory = @"C:\Sourcecode\temp";

        private static readonly HttpClient client = new HttpClient();
        private const string ApiUrl = "http://localhost:8080/v1/chat/completions";

        private static List<Message> _history = new List<Message>();
        private static string _sysPrompt = "You are an expert .NET developer. You have access to tools to read/write files and run shell commands. " +
                            "Always verify your work by running 'dotnet build'. If you see errors, analyze the output and fix the code. " +
                            $"You are working in '{WorkingDirectory}'. Ignore `bin`, `obj`, `.git`, `.obsidian`, and `.vs` directories. ";

        public static async Task Main(string[] args)
        {
            client.Timeout = TimeSpan.FromMinutes(5);

            Console.WriteLine("--- AI Autonomous Engineer PoC ---");
            SetWorkingDir();
            BootstrapContext();

            _history.Add(new Message("system", "Ask the user what they would like to do"));

            Console.WriteLine($"\nSYSTEM PROMPT: {_sysPrompt}\n");

            string? userInput = null;
            int loopCount = 0;
            do
            {
                userInput = GetUserInput();
                if (IsExit(userInput))
                    break;

                if (CompactContextRequested(userInput))
                {
                    string summary = await CompactContextAsync();
                    Console.WriteLine($"LAST SESSION SUMMARY:\n{summary}\n");
                    continue;
                }

                _history.Add(new Message("user", userInput));

                bool isRunning = true;
                int iterations = 0;
                const int maxIterations = 15;

                while (isRunning && iterations < maxIterations)
                {
                    iterations++;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"\n\tThinking...");

                    var response = await GetLLMResponse(_history);

                    Console.WriteLine($"\n{response}\n\n----------------------------------------------\n");

                    if (response?.Choices == null || response.Choices.Count == 0)
                    {
                        Console.WriteLine($"AI Response was null or contained no Choices");
                        isRunning = false;
                        continue;
                    }

                    var aiMessage = response.Choices[0].Message;

                    // CASE 1: The AI wants to use tools
                    if (aiMessage?.ToolCalls != null && aiMessage.ToolCalls.Count > 0)
                    {
                        foreach (var toolCall in aiMessage.ToolCalls)
                        {
                            Console.WriteLine($"\tAI requested tool: {toolCall.Function.Name} with args {toolCall.Function.Arguments}");

                            string result = ExecuteTool(toolCall.Function.Name, toolCall.Function.Arguments);

                            // Add the AI's request to history (Crucial for context)
                            _history.Add(aiMessage);

                            // Add the Tool output to history
                            _history.Add(new Message("tool", result, toolCall.Id));
                        }
                    }
                    // CASE 2: The AI has provided a final answer (or is talking to us)
                    else if (!string.IsNullOrWhiteSpace(aiMessage?.Content))
                    {
                        Console.WriteLine($"\n{aiMessage.Content}");
                        isRunning = false;
                    }
                    // CASE 3: Empty response from LLM
                    else
                    {
                        Console.WriteLine("AI returned an empty response.");
                        isRunning = false;
                    }
                    Console.WriteLine();
                }

            } while (loopCount < 100);

        }

        private static void BootstrapContext()
        {
            _history = new List<Message>
            {
                new Message("system", _sysPrompt),
            };

            string? readMeContents = GetIndexContents();
            if (readMeContents != null)
                _history.Add(new Message("context", readMeContents));
        }

        private static async Task<string> CompactContextAsync()
        {
            _history.Add(new Message("user", "Summarize this session in under 200 characters"));
            var response = await GetLLMResponse(_history);
            string? summary = response.Choices.FirstOrDefault()?.Message.Content;

            BootstrapContext();

            if(summary != null) 
                _history.Add(new Message("system", $"Previous session summary: {summary}"));

            return summary ?? "No summary was generated";
        }

        private static bool CompactContextRequested(string input) => input.Trim().Equals("compact", StringComparison.InvariantCultureIgnoreCase);

        private static bool IsExit(string input) => input.Trim().Equals("exit", StringComparison.InvariantCultureIgnoreCase);

        private static string GetUserInput()
        {
            string? userInput;
            var origColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;

            do
            {
                Console.Write($"CM {WorkingDirectory}> ");
                userInput = Console.ReadLine();
            } while (string.IsNullOrWhiteSpace(userInput));

            Console.ForegroundColor = origColor;
            return userInput;
        }

        private static void SetWorkingDir()
        {
            string? input = null;
            int loopCount = 0;
            do
            {
                Console.Write("\nEnter Directory: ");
                input = Console.ReadLine();

                if (Directory.Exists(input))
                {
                    WorkingDirectory = input;
                    return;
                }

                var origColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t!!! Invalid directory !!!");
                Console.ForegroundColor = origColor;

                loopCount++;
            } while (loopCount < 100);

            throw new FileNotFoundException();
        }

        private static string? GetIndexContents()
        {
            string result = ToolReadFile("INDEX.md");
            return result.Equals(_fileNotFound) ? null : result;
        }

        private static bool IsPath(string path) =>
    !string.IsNullOrWhiteSpace(path) &&
    path.IndexOfAny(Path.GetInvalidPathChars()) == -1;

        private static string ExecuteTool(string name, string argsJson)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, string>>(argsJson);
                return name switch
                {
                    "write_file" => ToolWriteFile(args["path"], args["content"]),
                    "read_file" => ToolReadFile(args["path"]),
                    "run_command" => ToolRunCommand(args["command"]),
                    _ => $"Error: Tool {name} not found."
                };
            }
            catch (Exception ex)
            {
                return $"Error executing tool {name}: {ex.Message}";
            }
        }

        #region Tools Implementation
        static string ToolWriteFile(string path, string content)
        {
            string fullPath = Path.IsPathRooted(path)
                              ? path
                              : Path.Combine(WorkingDirectory, path);

            File.WriteAllText(fullPath, content);
            return $"Successfully wrote to {fullPath}";
        }

        private static string _fileNotFound = "File not found.";
        static string ToolReadFile(string path)
        {
            string fullPath = Path.IsPathRooted(path)
                              ? path
                              : Path.Combine(WorkingDirectory, path);

            return File.Exists(fullPath) ? File.ReadAllText(fullPath) : _fileNotFound;
        }

        static string ToolRunCommand(string command)
        {
            if (!Directory.Exists(WorkingDirectory))
            {
                return $"Error: The working directory '{WorkingDirectory}' does not exist.";
            }

            var processInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                WorkingDirectory = WorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(processInfo);
                string output = process.StandardOutput.ReadToEnd();
                string error = process. StandardError.ReadToEnd();
                process.WaitForExit();

                return string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(error)
                       ? "Command executed with no output."
                       : $"{output}\n{error}".Trim();
            }
            catch (Exception ex)
            {
                return $"Failed to execute command: {ex.Message}";
            }
        }
        #endregion

        private static async Task<ChatResponse> GetLLMResponse(List<Message> history)
        {
            var requestBody = new
            {
                model = "gemma",
                messages = history,
                tools = GetToolDefinitions(),
                tool_choice = "auto"
            };

            string resultString = string.Empty;
            int loopCount = 0;
            do
            {
                try
                {
                    var content = JsonSerializer.Serialize(requestBody);
                    var response = await client.PostAsync(ApiUrl, new StringContent(content, Encoding.UTF8, "application/json"));
                    resultString = await response.Content.ReadAsStringAsync();

                    Debug.Indent();
                    Debug.WriteLine($"-------------");
                    Debug.WriteLine($"INPUT:\n{content}\n\nRESPONSE:\n{resultString}");
                    Debug.WriteLine($"-------------");
                    Debug.Unindent();

                    break;
                }
                catch (Exception ex)
                {
                    resultString = ex.Message;
                }

                loopCount++;
            } while (loopCount < 2);

            return JsonSerializer.Deserialize<ChatResponse>(resultString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        private static List<object> GetToolDefinitions()
        {
            return new List<object>
        {
            new {
                type = "function",
                function = new {
                    name = "write_file",
                    description = "Writes content to a file at the specified path.",
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
                    description = "Reads the content of a file.",
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
                    name = "run_command",
                    description = "Runs a shell command (e.g., 'dotnet build').",
                    parameters = new {
                        type = "object",
                        properties = new {
                            command = new { type = "string", description = "The shell command to execute" }
                        },
                        required = new[] { "command" }
                    }
                }
            }
        };
        }
    }
}
