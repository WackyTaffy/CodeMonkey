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
            var manifestService = new ManifestService();
            var userPreferences = new UserPreferences();
            var sessionLedger = new SessionLedger();

            _toolManager = new ToolManager(_fileSystem, _shell, manifestService, userPreferences, sessionLedger);
            _conversationManager = new ConversationManager();
            _orchestrator = new Orchestrator(_llmClient, _toolManager, _fileSystem, _conversationManager)
            {
                Verbose = verbose
            };

            WriteLog("--- AI Autonomous Engineer PoC ---");
            if (verbose) WriteLog("[Mode] Verbose output enabled");
            SetWorkingDir();

            _orchestrator.BootstrapContext(WorkingDirectory);

            WriteLog($"\nSYSTEM PROMPT: {_orchestrator.GetSystemPrompt(WorkingDirectory)}\n");

            // Subscribe to orchestrator status updates
            _orchestrator.OnStatusUpdate = (status) => 
            {
                WriteLog($"[STATUS] {status}");
            };

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

                string response = await _orchestrator.ProcessUserRequestAsync(userInput, WorkingDirectory);
                WriteAiResponse($"\n{response}\n");

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
            Console.WriteLine(str);
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
