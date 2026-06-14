using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System.Text;

namespace CodeMonkey.Core.Services
{
    public interface IOrchestrator
    {
        Task<string> ProcessUserRequestAsync(string userInput, string workingDirectory, List<Message> history);
        Task<string> CompactContextAsync(List<Message> history, string workingDirectory);
        void BootstrapContext(List<Message> history, string workingDirectory);
    }

    public class Orchestrator : IOrchestrator
    {
        private readonly ILLMClient _llmClient;
        private readonly IToolManager _toolManager;
        private readonly IFileSystem _fileSystem;

        public Orchestrator(ILLMClient llmClient, IToolManager toolManager, IFileSystem fileSystem)
        {
            _llmClient = llmClient;
            _toolManager = toolManager;
            _fileSystem = fileSystem;
        }

        public void BootstrapContext(List<Message> history, string workingDirectory)
        {
            string sysPrompt = $"You are an expert .NET developer. You have access to tools to read/write files and run shell commands. " +
                               $"Always verify your work by running 'dotnet build'. If you see errors, analyze the output and fix the code. " +
                               $"You are working in '{workingDirectory}'. ";

            history.Clear();
            history.Add(new Message("system", sysPrompt));

            string readMeContents = _fileSystem.ReadFile("INDEX.md", workingDirectory);
            if (!readMeContents.Contains("File not found"))
            {
                history.Add(new Message("context", readMeContents));
            }
        }

        public async Task<string> CompactContextAsync(List<Message> history, string workingDirectory)
        {
            history.Add(new Message("user", "Summarize this session in under 200 characters"));
            var response = await _llmClient.GetChatCompletionAsync(history);
            string? summary = response?.Choices?.FirstOrDefault()?.Message?.Content;

            BootstrapContext(history, workingDirectory);

            if (summary != null)
                history.Add(new Message("system", $"Previous session summary: {summary}"));

            return summary ?? "No summary was generated";
        }

        public async Task<string> ProcessUserRequestAsync(string userInput, string workingDirectory, List<Message> history)
        {
            history.Add(new Message("user", userInput));

            bool isRunning = true;
            int iterations = 0;
            const int maxIterations = 15;

            while (isRunning && iterations < maxIterations)
            {
                iterations++;
                var response = await _llmClient.GetChatCompletionAsync(history);

                if (response?.Choices == null || response.Choices.Count == 0)
                {
                    return "AI Response was null or contained no Choices";
                }

                var aiMessage = response.Choices[0].Message;

                if (aiMessage?.ToolCalls != null && aiMessage.ToolCalls.Count > 0)
                {
                    foreach (var toolCall in aiMessage.ToolCalls)
                    {
                        string result = _toolManager.ExecuteTool(toolCall.Function.Name, toolCall.Function.Arguments, workingDirectory);
                        history.Add(aiMessage);
                        history.Add(new Message("tool", result, toolCall.Id));
                    }
                }
                else if (!string.IsNullOrWhiteSpace(aiMessage?.Content))
                {
                    return aiMessage.Content;
                }
                else
                {
                    return "AI returned an empty response.";
                }
            }

            return "Reached maximum iterations without a final answer.";
        }
    }
}
