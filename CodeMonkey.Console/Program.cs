using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeMonkey.Cli
{
    public class Program
    {
        public static string WorkingDirectory = @"C:\Sourcecode\temp";

        private static readonly HttpClient client = new HttpClient();
        private const string ApiUrl = "http://localhost:8080/v1/chat/completions";

        public static async Task Main(string[] args)
        {
            Console.WriteLine("--- AI Autonomous Engineer PoC ---");
            SetWorkingDir();

            string sysPrompt = "You are an expert .NET developer. You have access to tools to read/write files and run shell commands. " +
                                "Always verify your work by running 'dotnet build'. If you see errors, analyze the output and fix the code. " +
                                $"You are working in '{WorkingDirectory}'. ";

            var history = new List<Message>
            {
                new Message("system", sysPrompt),
            };

            string? readMeContents = GetReadMeContents();
            if (readMeContents != null)
                history.Add(new Message("context", readMeContents));

            history.Add(new Message("system", "Ask the user what they would like to do"));


            Console.WriteLine($"\nSYSTEM PROMPT: {sysPrompt}\n");

            string? userInput = null;
            int loopCount = 0;
            do
            {
                userInput = GetUserInput();
                if (IsExit(userInput))
                    break;

                history.Add(new Message("user", userInput));

                bool isRunning = true;
                int iterations = 0;
                const int maxIterations = 15;

                while (isRunning && iterations < maxIterations)
                {
                    iterations++;
                    Console.WriteLine($"\n\tThinking...");

                    var response = await GetLLMResponse(history);

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
                            history.Add(aiMessage);

                            // Add the Tool output to history
                            history.Add(new Message("tool", result, toolCall.Id));
                        }
                        // isRunning remains true; we loop back to see if the AI needs more tools 
                        // based on the results of the commands just executed.
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

        private static string? GetReadMeContents()
        {
            string result = ToolReadFile("README.md");
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
            // Combine with WorkingDirectory if the AI provided a relative path
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
            // 1. Safety Check: Ensure the directory exists
            if (!Directory.Exists(WorkingDirectory))
            {
                return $"Error: The working directory '{WorkingDirectory}' does not exist.";
            }

            var processInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                // 2. Set the Working Directory here
                WorkingDirectory = WorkingDirectory,

                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(processInfo);

                // Read output and error streams
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // In the context of 'dotnet build', errors often go to StandardOutput 
                // as well as StandardError, so we combine them for the LLM.
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

            var content = JsonSerializer.Serialize(requestBody);
            var response = await client.PostAsync(ApiUrl, new StringContent(content, Encoding.UTF8, "application/json"));
            var resultString = await response.Content.ReadAsStringAsync();

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

    #region API Models (Fixed with Explicit JSON Mapping)

    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        // FIX: Ignore when null because tool_call messages might not have content
        [JsonPropertyName("content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Content { get; set; }

        // FIX: This was the cause of your 500 error. 
        // System/User messages MUST NOT send "tool_call_id": null
        [JsonPropertyName("tool_call_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ToolCallId { get; set; }

        [JsonPropertyName("tool_calls")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ToolCall> ToolCalls { get; set; }

        public Message(string role, string content, string toolCallId = null)
        {
            Role = role;
            Content = content;
            ToolCallId = toolCallId;
        }

        public Message() { }

        public override string ToString() => $"[{Role}] {ToolCalls?.Count ?? 0} Tool Calls, Content Length = {Content.Length}";
    }

    public class ToolCall
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        // FIX: The server requires the "type" field (usually "function") 
        // to be present in the history when sending messages back.
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        [JsonPropertyName("function")]
        public FunctionCall Function { get; set; }
    }

    public class FunctionCall
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("arguments")]
        public string Arguments { get; set; }
    }

    public class ChatResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; }

        public override string ToString() => string.Join("\n", Choices);
    }

    public class Choice
    {
        [JsonPropertyName("message")]
        public Message Message { get; set; }

        public override string ToString() => Message.ToString();
    }

    #endregion

}
