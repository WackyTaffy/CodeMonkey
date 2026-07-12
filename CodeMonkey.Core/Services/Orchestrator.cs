using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;

namespace CodeMonkey.Core.Services
{
    public class Orchestrator : IOrchestrator
    {
        private readonly IAgentExecutor _agentExecutor;
        private readonly IPromptProvider _promptProvider;
        private readonly IFileSystem _fileSystem;
        private readonly IConversationManager _conversationManager;

        public Action<string>? OnStatusUpdate { get; set; }
        public Action<ToolResult>? OnToolExecuted { get; set; }
        public bool Verbose { get; set; }

        public Orchestrator(
            IAgentExecutor agentExecutor, 
            IPromptProvider promptProvider, 
            IFileSystem fileSystem, 
            IConversationManager conversationManager)
        {
            _agentExecutor = agentExecutor;
            _promptProvider = promptProvider;
            _fileSystem = fileSystem;
            _conversationManager = conversationManager;
        }

        public void BootstrapContext(string workingDirectory)
        {
            string sysPrompt = _promptProvider.GetSystemPrompt(workingDirectory);
            _conversationManager.AddMessage(Message.AsSystemPrompt(sysPrompt));

            string indexContents = _fileSystem.ReadFile("INDEX.md", workingDirectory);
            if (!indexContents.Contains("File not found"))
            {
                _conversationManager.AddMessage(Message.AsFileContents(indexContents));
            }

            string readmeContents = _fileSystem.ReadFile("README.md", workingDirectory);
            if (!readmeContents.Contains("File not found"))
            {
                _conversationManager.AddMessage(Message.AsFileContents(readmeContents));
            }
        }

        public async Task<string> CompactContextAsync(string workingDirectory)
        {
            return await _conversationManager.CompactAsync(
                _agentExecutor.Client, 
                _promptProvider.GetSystemPrompt(workingDirectory));
        }

        public async Task<ToolResult> ProcessUserRequestAsync(string userInput, string workingDirectory)
        {
            _conversationManager.AddMessage(Message.AsUserMessage(userInput));

            return await _agentExecutor.ExecuteLoopAsync(
                "Main Agent", 
                _conversationManager, 
                workingDirectory, 
                null, 
                (status) => OnStatusUpdate?.Invoke(status), 
                (toolResult) => OnToolExecuted?.Invoke(toolResult), 
                _promptProvider.GetSystemPrompt(workingDirectory));
        }

        public string GetSystemPrompt(string workingDirectory)
        {
            return _promptProvider.GetSystemPrompt(workingDirectory);
        }
    }
}
