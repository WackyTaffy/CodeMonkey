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
        
        private static ILLMClient _llmClient = null!;
        private static IShell _shell = null!;
        private static IFileSystem _fileSystem = null!;
        private static IToolManager _toolManager = null!;
        private static IOrchestrator _orchestrator = null!;
        private static IConversationManager _conversationManager = null!;

        public static async Task Main(string[] args)
        {
            bool verbose = true;// args.Contains("--verbose");

            _client.Timeout = TimeSpan.FromMinutes(5);
            _llmClient = new LLMClient(_client);
            _fileSystem = new Core.Services.FileSystem();
            _shell = new Shell();

            // Initialize security services
            var userPreferences = new UserPreferences();
            
            var tokenHelper = new GemmaTokenHelper();
            var contextGuard = new ContextGuard(tokenHelper);

            _toolManager = new ToolManager(_fileSystem, _shell, userPreferences, tokenHelper);
            _conversationManager = new ConversationManager();

            // Modular Services DI
            var promptProvider = new PromptProvider();
            var subagentManager = new SubagentManager(promptProvider, _fileSystem, _toolManager);
            var toolDispatcher = new ToolDispatcher(_toolManager, subagentManager, tokenHelper);
            var agentExecutor = new AgentExecutor(_llmClient, toolDispatcher, _conversationManager);
            
            // Break circular dependency
            subagentManager.SetExecutor(agentExecutor);

            _orchestrator = new Orchestrator(agentExecutor, promptProvider, _fileSystem, _conversationManager)
            {
                Verbose = verbose
            };

            Console.WriteLine("--- AI Autonomous Engineer PoC ---");
            if (verbose) Console.WriteLine("[Mode] Verbose output enabled");
            SetWorkingDir();

            _orchestrator.BootstrapContext(WorkingDirectory);

            // Note: GetSystemPrompt moved to promptProvider, but we can still get it via promptProvider
            WriteLog($"\nSYSTEM PROMPT: {promptProvider.GetSystemPrompt(WorkingDirectory)}\n");

            // Subscribe to orchestrator and subagentManager status updates
            Action<string> statusUpdateAction = (status) => 
            {
                if (status.StartsWith("[REASONING]"))
                {
                    WriteReasoning(status.Replace("[REASONING]", ""));
                }
                else
                {
                    WriteLog($"[STATUS] {status}");
                }
            };
            _orchestrator.OnStatusUpdate = statusUpdateAction;
            subagentManager.OnStatusUpdate = statusUpdateAction;

            // Subscribe to orchestrator and subagentManager tool results
            Action<ToolResult> toolResultAction = (result) =>
            {
                WriteLog($"[TOOL] {result.ToStringShort()}");
            };
            _orchestrator.OnToolExecuted = toolResultAction;
            subagentManager.OnToolExecuted = toolResultAction;

            string? userInput = null;
            int loopCount = 0;
            do
            {
                int currentTokens = _conversationManager.GetTotalTokenCount();

                userInput = GetUserInput(currentTokens);
                if (IsExit(userInput))
                    break;

                if (CompactContextRequested(userInput))
                {
                    string summary = await _orchestrator.CompactContextAsync(WorkingDirectory);
                    WriteLog($"LAST SESSION SUMMARY:\n{summary}\n");
                    continue;
                }

                ToolResult response = await _orchestrator.ProcessUserRequestAsync(userInput, WorkingDirectory);
                WriteAiResponse($"\n{response.ToString()}\n");

                loopCount++;
            } while (loopCount < 100);

        }

        private static bool CompactContextRequested(string input) => input.Trim().Equals("compact", StringComparison.InvariantCultureIgnoreCase);

        private static bool IsExit(string input) => input.Trim().Equals("exit", StringComparison.InvariantCultureIgnoreCase);

        private static string GetUserInput(int tokenCount)
        {
            string? userInput;
            var origColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;

            do
            {
                Console.Write($"CM {WorkingDirectory} [{tokenCount} tokens]> ");
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
                Console.ForegroundColor = Console.ForegroundColor;
                Console.WriteLine();
                Console.ForegroundColor = origColor;

                loopCount++;
            } while (loopCount < 100);

            throw new FileNotFoundException();
        }

        private static void WriteLog(string str)
        {
            var origColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"-- {_conversationManager.GetTotalTokenCount()} -- {str}");
            Console.ForegroundColor = origColor;
        }

        private static void WriteReasoning(string str)
        {
            var origColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"-- {_conversationManager.GetTotalTokenCount()} -- [REASONING] {str}");
            Console.ForegroundColor = origColor;
        }

        private static void WriteAiResponse(string str)
        {
            var origColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(str);
            Console.ForegroundColor = origColor;
        }
    }
}
