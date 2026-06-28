using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System.Text;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeMonkey.Core.Services
{
    public class Orchestrator : IOrchestrator
    {
        private readonly ILLMClient _llmClient;
        private readonly IToolManager _toolManager;
        private readonly IFileSystem _fileSystem;
        private readonly IConversationManager _conversationManager;
        private const int TokenLimit = 12500;

        public Action<string>? OnStatusUpdate { get; set; }
        public bool Verbose { get; set; }

        public Orchestrator(ILLMClient llmClient, IToolManager toolManager, IFileSystem fileSystem, IConversationManager conversationManager)
        {
            _llmClient = llmClient;
            _toolManager = toolManager;
            _fileSystem = fileSystem;
            _conversationManager = conversationManager;
        }

        public string GetSystemPrompt(string workingDirectory)
        {
            return $"You are an expert .NET developer. You have access to tools to read/write files, run shell commands, and dispatch subagents. " +
                   $"Verify code generation by running 'dotnet build' and 'dotnet test'. " +
                   $"You are working in '{workingDirectory}'.\n\n" +
                   $"Subagent Dispatch:\n" +
                   $"Use subagents via 'dispatch_subagent' for repetitive exploration, summarizing data, or tasks that would generate excessive tool output. " +
                   $"Clearly define the task and grant only necessary permissions (e.g., 'write_file') if the subagent needs to modify the codebase. " +
                   $"Provide a list of files the subagent should start with to minimize unnecessary tool calls. " +
                   $"Subagents return only their final result, keeping your context clean.";
        }

        public void BootstrapContext(string workingDirectory)
        {
            string sysPrompt = GetSystemPrompt(workingDirectory);
            _conversationManager.AddMessage(new Message("system", sysPrompt));

            string readMeContents = _fileSystem.ReadFile("INDEX.md", workingDirectory);
            if (!readMeContents.Contains("File not found"))
            {
                _conversationManager.AddMessage(new Message("context", readMeContents));
            }
        }

        public async Task<string> CompactContextAsync(string workingDirectory)
        {
            return await _conversationManager.CompactAsync(_llmClient, GetSystemPrompt(workingDirectory));
        }

        public async Task<string> ProcessUserRequestAsync(string userInput, string workingDirectory)
        {
            return await RunAgentLoopAsync(userInput, workingDirectory, null);
        }

        private async Task<ChatResponse?> GetResponseWithRetryAsync(List<Message> messages, string agentLabel, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = await _llmClient.GetChatCompletionAsync(messages);
                    if (response?.Choices != null && response.Choices.Count > 0)
                    {
                        return response;
                    }
                    if (Verbose) OnStatusUpdate?.Invoke($"[{agentLabel}] [VERBOSE] AI response was empty or null. Retry {i + 1}/{maxRetries}...");
                }
                catch (Exception ex)
                {
                    if (Verbose) OnStatusUpdate?.Invoke($"[{agentLabel}] [VERBOSE] LLM call failed: {ex.Message}. Retry {i + 1}/{maxRetries}...");
                }

                if (i < maxRetries - 1)
                {
                    await Task.Delay(1000 * (i + 1)); // Exponential backoff
                }
            }
            return null;
        }

        private async Task<string> RunAgentLoopAsync(string userInput, string workingDirectory, List<string>? permissions)
        {
            _conversationManager.AddMessage(new Message("user", userInput));

            int iterations = 0;

            while (true)
            {
                iterations++;
                OnStatusUpdate?.Invoke($"[Main Agent] Iteration {iterations}: Thinking...");
                
                if (Verbose)
                {
                    int currentTokens = _conversationManager.GetTotalTokenCount();
                    OnStatusUpdate?.Invoke($"[Main Agent] [VERBOSE] Current Context Window: {currentTokens} tokens");
                    OnStatusUpdate?.Invoke($"[Main Agent] [VERBOSE] Message count: {_conversationManager.GetMessages().Count()}");
                }

                var messages = _conversationManager.GetMessages().ToList();
                var response = await GetResponseWithRetryAsync(messages, "Main Agent");

                if (response == null || response.Choices == null || response.Choices.Count == 0)
                {
                    return "AI Response was null or contained no Choices after multiple retries.";
                }

                var aiMessage = response.Choices[0].Message;

                if (aiMessage?.ToolCalls != null && aiMessage.ToolCalls.Count > 0)
                {
                    _conversationManager.AddMessage(aiMessage);

                    foreach (var toolCall in aiMessage.ToolCalls)
                    {
                        if (_conversationManager.ShouldCompact(TokenLimit))
                        {
                            OnStatusUpdate?.Invoke($"[Main Agent] Context limit reached ({_conversationManager.GetTotalTokenCount()}/{TokenLimit}). Compacting context...");
                            await CompactContextAsync(workingDirectory);
                        }

                        OnStatusUpdate?.Invoke($"[Main Agent] Calling tool: {toolCall.Function.Name} with args: {toolCall.Function.Arguments}");
                        if (toolCall.Function.Name == "dispatch_subagent")
                        {
                            if (permissions != null)
                            {
                                _conversationManager.AddMessage(new Message("tool", "Error: Subagents cannot dispatch further subagents.", toolCall.Id));
                                continue;
                            }

                            string result = await HandleSubagentDispatchAsync(toolCall.Function.Arguments, workingDirectory);
                            _conversationManager.AddMessage(new Message("tool", result, toolCall.Id));
                        }
                        else
                        {
                            string result = _toolManager.ExecuteTool(toolCall.Function.Name, toolCall.Function.Arguments, workingDirectory, permissions);
                            _conversationManager.AddMessage(new Message("tool", result, toolCall.Id));
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(aiMessage?.Content))
                {
                    _conversationManager.AddMessage(aiMessage);
                    return aiMessage.Content;
                }
                else
                {
                    if (Verbose) OnStatusUpdate?.Invoke("[Main Agent] [VERBOSE] AI returned an empty response with no tool calls");
                    return "AI returned an empty response.";
                }

                if (_conversationManager.ShouldCompact(TokenLimit))
                {
                    OnStatusUpdate?.Invoke($"[Main Agent] Context limit reached ({_conversationManager.GetTotalTokenCount()}/{TokenLimit}). Compacting context...");
                    await CompactContextAsync(workingDirectory);
                }
            }
        }

        private async Task<string> HandleSubagentDispatchAsync(string argsYaml, string workingDirectory)
        {
            try
            {
                var args = _toolManager.ParseArguments<SubagentDispatchArgs>(argsYaml);
                if (args == null) return "Error: Invalid arguments for subagent dispatch";

                OnStatusUpdate?.Invoke($"[Main Agent] Dispatching subagent '{args.Name}' for task: {args.Task}");

                List<Message> subagentHistory = new List<Message>();
                string subagentSysPrompt = $"You are a specialized subagent named '{args.Name}'. Your goal is: {args.Task}. " +
                                          $"You are working in '{workingDirectory}'. " +
                                          $"Return only the final result of your task.";
                subagentHistory.Add(new Message("system", subagentSysPrompt));

                var contextBuilder = new StringBuilder();
                contextBuilder.AppendLine("--- INITIAL CONTEXT ---");
                contextBuilder.AppendLine($"Task: {args.Task}");
                contextBuilder.AppendLine("\nRelevant Files:");
                foreach (var filePath in args.InitialContext)
                {
                    string content = _fileSystem.ReadFile(filePath, workingDirectory);
                    contextBuilder.AppendLine($"\nFile: {filePath}\nContent:\n{content}\n---");
                }
                contextBuilder.AppendLine("\n--- END INITIAL CONTEXT ---");
                subagentHistory.Add(new Message("context", contextBuilder.ToString()));
                
                return await RunSubagentLoopAsync(args.Name, args.Task, workingDirectory, subagentHistory, args.Permissions);
            }
            catch (Exception ex)
            {
                return $"Error dispatching subagent: {ex.Message}";
            }
        }

        private async Task<string> RunSubagentLoopAsync(string agentName, string userInput, string workingDirectory, List<Message> history, List<string>? permissions)
        {
            history.Add(new Message("user", userInput));
            int iterations = 0;

            while (true)
            {
                iterations++;
                OnStatusUpdate?.Invoke($"[{agentName}] Iteration {iterations}: Thinking...");
                
                var response = await GetResponseWithRetryAsync(history, agentName);
                if (response == null || response.Choices == null || response.Choices.Count == 0) 
                {
                    return "Subagent response null or empty after multiple retries";
                }

                var aiMessage = response.Choices[0].Message;
                if (aiMessage?.ToolCalls != null && aiMessage.ToolCalls.Count > 0)
                {
                    history.Add(aiMessage);
                    foreach (var toolCall in aiMessage.ToolCalls)
                    {
                        OnStatusUpdate?.Invoke($"[{agentName}] Calling tool: {toolCall.Function.Name}");
                        string result = _toolManager.ExecuteTool(toolCall.Function.Name, toolCall.Function.Arguments, workingDirectory, permissions);
                        history.Add(new Message("tool", result, toolCall.Id));
                    }
                }
                else if (!string.IsNullOrWhiteSpace(aiMessage?.Content))
                {
                    history.Add(aiMessage);
                    return aiMessage.Content;
                }
                else 
                {
                    if (Verbose) OnStatusUpdate?.Invoke($"[{agentName}] [VERBOSE] AI returned empty response");
                    return "Subagent returned empty response";
                }
            }
        }
    }
}
