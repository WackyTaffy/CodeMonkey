using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using CodeMonkey.Core.Services;
using CodeMonkey.Core.Utility;

namespace CodeMonkey.Cli
{
    public class Program
    {
        public static string WorkingDirectory = @"C:\Sourcecode\temp";

        private static readonly HttpClient _client = new HttpClient();
        private const string ApiUrl = "http://localhost:8080/v1/chat/completions";

        private static List<Message> _mainAgentContext = new List<Message>();

        private static ILLMClient _llmClient;
        private static IShell _shell;
        private static IFileSystem _fileSystem;
        private static IToolManager _toolManager;
        private static IOrchestrator _orchestrator;
        private static GemmaTokenHelper _tokenHelper;

        private static readonly List<string> _invalidDir = new() { "bin", "obj" };


        private static string _sysPrompt => "You are an expert .NET developer. You have access to tools to read/write files and run shell commands. " +
                            "Always verify your work by running 'dotnet build'. If you see errors, analyze the output and fix the code. " +
                            $"You are working in '{WorkingDirectory}'. ";

        public static async Task Main(string[] args)
        {
            _client.Timeout = TimeSpan.FromMinutes(5);
            _llmClient = new LLMClient(_client);
            _fileSystem = new Core.Services.FileSystem();
            _shell = new Shell();
            _toolManager = new ToolManager(_fileSystem, _shell);
            _orchestrator = new Orchestrator(_llmClient, _toolManager, _fileSystem);
            _tokenHelper = new GemmaTokenHelper();

            WriteLog("--- AI Autonomous Engineer PoC ---");
            SetWorkingDir();

            _orchestrator.BootstrapContext(_mainAgentContext, WorkingDirectory);

            WriteLog($"\nSYSTEM PROMPT: {_sysPrompt}\n");

            string? userInput = null;
            int loopCount = 0;
            do
            {
                userInput = GetUserInput();
                if (IsExit(userInput))
                    break;

                if (CompactContextRequested(userInput))
                {
                    string summary = await _orchestrator.CompactContextAsync(_mainAgentContext, WorkingDirectory);
                    WriteLog($"LAST SESSION SUMMARY:\n{summary}\n");
                    continue;
                }

                _mainAgentContext.Add(new Message("user", userInput));

                bool isRunning = true;
                int iterations = 0;
                const int maxIterations = 15;

                while (isRunning && iterations < maxIterations)
                {
                    iterations++;

                    Console.ForegroundColor = ConsoleColor.White;
                    WriteLog($"\n\tThinking...");

                    ChatResponse response = await _llmClient.GetChatCompletionAsync(_mainAgentContext);

                    WriteLog($"\n{response}\n");

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
                            WriteLog($"\tTool: {toolCall.Function.Name} " +
                                $"with args '{toolCall.Function.Arguments}'");

                            //string result = ExecuteTool(toolCall.Function.Name, toolCall.Function.Arguments);
                            string result = _toolManager.ExecuteTool(toolCall.Function.Name, toolCall.Function.Arguments, WorkingDirectory);

                            WriteLog($"\tResult: {result}");

                            // Add the AI's request to history (Crucial for context)
                            _mainAgentContext.Add(aiMessage);

                            // Add the Tool output to history
                            _mainAgentContext.Add(new Message("tool", result, toolCall.Id));
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
                    WriteLog($"\n----------------------------------------------\n");
                }

            } while (loopCount < 100);

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

        private static void WriteLog(string str)
        {
            var origColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(str);
            Console.ForegroundColor = origColor;
        }
    }
}
