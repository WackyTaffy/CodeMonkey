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
                               $"Provide a list of files the subagent should start with to minimize unnecessary tool calls. " +
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
                var args = _toolManager.ParseArguments<SubagentDispatchArgs>(argsYaml);
                if (args == null) return "Error: Invalid arguments for subagent dispatch";

                List<Message> subagentHistory = new List<Message>();
                
                // Subagent System Prompt
                string subagentSysPrompt = $"You are a specialized subagent. Your goal is: {args.Task}. " +
                                          $"You must report all changes made. If you fail, report partial progress. " +
                                          $"You are working in '{workingDirectory}'.";
                
                subagentHistory.Add(new Message("system", subagentSysPrompt));

                // Context Injection: Prompt + Suggested Files
                var contextBuilder = new StringBuilder();
                contextBuilder.AppendLine("--- INITIAL CONTEXT ---");
                contextBuilder.AppendLine($"Task: {args.Task}");
                contextBuilder.AppendLine("\nRelevant Files:");
                foreach (var filePath in args.InitialContext)
                {
                    string content = _fileSystem.ReadFile(filePath, workingDirectory);
                    if (!content.Contains("File not found"))
                    {
                        contextBuilder.AppendLine($"\n--- File: {filePath} ---\n{content}");
                    }
                    else
                    {
                        contextBuilder.AppendLine($"\n--- File: {filePath} ---\n(File not found)");
                    }
                }
                contextBuilder.AppendLine("\n--- END INITIAL CONTEXT ---");

                subagentHistory.Add(new Message("context", contextBuilder.ToString()));
                
                // Start the loop with the task as the user input to the subagent
                return await RunAgentLoopAsync(args.Task, workingDirectory, subagentHistory, args.Permissions);
            }
            catch (Exception ex)
            {
                return $"Error dispatching subagent: {ex.Message}";
            }
        }
    }
}
