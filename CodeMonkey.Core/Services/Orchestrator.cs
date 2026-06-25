using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System.Text;

namespace CodeMonkey.Core.Services
{
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
                               $"You are working in '{workingDirectory}'.\n\n" +
                               $"Subagent Dispatch:\n" +
                               $"Use subagents via 'dispatch_subagent' for repetitive exploration, summarizing large volumes of data, or tasks that would generate excessive tool output. " +
                               $"Clearly define the task and grant only necessary permissions (e.g., 'write_file') if the subagent needs to modify the codebase. " +
                               $"Subagents return only their final result, keeping your context clean.";

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
            return await RunAgentLoopAsync(userInput, workingDirectory, history, null);
        }

        private async Task<string> RunAgentLoopAsync(string userInput, string workingDirectory, List<Message> history, List<string>? permissions)
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
                        if (toolCall.Function.Name == "dispatch_subagent")
                        {
                            if (permissions != null)
                            {
                                history.Add(aiMessage);
                                history.Add(new Message("tool", "Error: Subagents cannot dispatch further subagents.", toolCall.Id));
                                continue;
                            }

                            string result = await HandleSubagentDispatchAsync(toolCall.Function.Arguments, workingDirectory);
                            history.Add(aiMessage);
                            history.Add(new Message("tool", result, toolCall.Id));
                        }
                        else
                        {
                            string result = _toolManager.ExecuteTool(toolCall.Function.Name, toolCall.Function.Arguments, workingDirectory, permissions);
                            history.Add(aiMessage);
                            history.Add(new Message("tool", result, toolCall.Id));
                        }
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

        private async Task<string> HandleSubagentDispatchAsync(string argsYaml, string workingDirectory)
        {
            try
            {
                // We can't use the ToolManager's deserializer directly because it's private/internal-ish
                // and we need to parse the arguments here. Let's assume it's a simple YAML.
                // For simplicity in this implementation, I'll use a basic parse or 
                // ideally the ToolManager should expose a way to deserialize.
                // Since I don't have a shared deserializer, I'll use a simple approach or 
                // add a helper to ToolManager.
                
                // Actually, I should add a Deserialize method to ToolManager.
                // For now, I'll use a quick and dirty way or modify ToolManager.
                
                // Let's assume we can parse it.
                // task: string, permissions: List<string>, initial_context: string
                
                // I will modify ToolManager to provide the arguments as a dictionary.
                // But let's just use a simple logic here for now.
                
                // I'll call a new method in ToolManager to get the dictionary.
                var args = _toolManager.ParseArguments(argsYaml);
                if (args == null) return "Error: Invalid arguments for subagent dispatch";

                string task = args.GetValueOrDefault("task", "No task provided");
                string permissionsStr = args.GetValueOrDefault("permissions", "");
                string initialContext = args.GetValueOrDefault("initial_context", "");

                List<string> permissions = string.IsNullOrWhiteSpace(permissionsStr) 
                    ? new List<string>() 
                    : permissionsStr.Split(',').Select(p => p.Trim()).ToList();

                List<Message> subagentHistory = new List<Message>();
                
                // Subagent System Prompt
                string subagentSysPrompt = $"You are a specialized subagent. Your goal is: {task}. " +
                                          $"You must report all changes made. If you fail, report partial progress. " +
                                          $"You are working in '{workingDirectory}'.";
                
                subagentHistory.Add(new Message("system", subagentSysPrompt));
                if (!string.IsNullOrWhiteSpace(initialContext))
                {
                    subagentHistory.Add(new Message("context", initialContext));
                }

                return await RunAgentLoopAsync(task, workingDirectory, subagentHistory, permissions);
            }
            catch (Exception ex)
            {
                return $"Error dispatching subagent: {ex.Message}";
            }
        }
    }
}
