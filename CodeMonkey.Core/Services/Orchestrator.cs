using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System.Text;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeMonkey.Core.Utility;

namespace CodeMonkey.Core.Services
{
    public class Orchestrator : IOrchestrator
    {
        private readonly ILLMClient _llmClient;
        private readonly IToolManager _toolManager;
        private readonly IFileSystem _fileSystem;
        private readonly IConversationManager _conversationManager;
        private readonly IContextGuard _contextGuard;
        private const int TokenLimit = 12500;
        private const int TotalTokenLimit = 15000;

        public Action<string>? OnStatusUpdate { get; set; }
        public Func<Guid, string, Task<bool>>? OnApprovalRequired { get; set; }
        public bool Verbose { get; set; }

        public Orchestrator(ILLMClient llmClient, IToolManager toolManager, IFileSystem fileSystem, IConversationManager conversationManager, IContextGuard contextGuard)
        {
            _llmClient = llmClient;
            _toolManager = toolManager;
            _fileSystem = fileSystem;
            _conversationManager = conversationManager;
            _contextGuard = contextGuard;
        }

        public string GetSystemPrompt(string workingDirectory)
        {
            return $@"You are an expert .NET developer working in '{workingDirectory}'. 
You have access to tools to read/write files, run shell commands, and dispatch subagents. Verify code generation by running 'dotnet build' and 'dotnet test'.

### 1. PHASED EXECUTION & HUMAN CHECKPOINTS
- When asked to investigate, analyze, or propose a solution, you must STOP immediately after presenting your proposal. 
- DO NOT begin implementation, code generation, or file modifications until the user explicitly responds with approval.
- Before executing any high-blast-radius or irreversible shell commands (e.g., deleting branches, destructive git actions), you must pause and ask for user confirmation.

### 2. SUBAGENT DISPATCH MATRIX
You must evaluate the ""blast radius"" and context size before executing tasks. Delegate to `dispatch_subagent` using these strict triggers:
- MANDATORY USE: Use subagents for multi-file discovery (e.g., searching for patterns across 5+ files), parsing massive log outputs, running repetitive test-fix loops, or handling isolated boilerplate generation.
- PROHIBITED USE: Do not delegate complex, multi-stage goals to a single subagent. Multi-stage goals must be decomposed into smaller objectives that will be fulfilled by individual subagents.
- DISPATCH PROTOCOL: Frame subagent tasks as single, atomic, narrow objectives. Provide them with a targeted, explicit list of starting files. Never pass a vague, multi-step roadmap to a subagent.

### 3. CONTEXT BUDGETING & PROGRESSIVE DISCLOSURE
- You operate under a strict {TotalTokenLimit} token context limit. You are forbidden from loading entire directories or performing recursive file searches that inclue `bin` and `obj` directories.
- PULL-ON-DEMAND: Treat 'INDEX.md', 'CONTEXT-MAP.md', and 'AGENTS.md' as shallow maps. Read them first for 1 session turn to identify which file or '.agents/' sub-directory contains the details you need.
";
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
                if (aiMessage == null) return "AI returned a null message.";

                // Handle dedicated reasoning content (e.g. DeepSeek/OpenAI reasoning_content)
                if (!string.IsNullOrWhiteSpace(aiMessage.ReasoningContent))
                {
                    OnStatusUpdate?.Invoke($"[REASONING] {aiMessage.ReasoningContent}");
                }

                if (aiMessage.ToolCalls != null && aiMessage.ToolCalls.Count > 0)
                {
                    // Handle pre-tool thoughts in the main content field
                    if (!string.IsNullOrWhiteSpace(aiMessage.Content))
                    {
                        OnStatusUpdate?.Invoke($"[REASONING] {aiMessage.Content}");
                    }

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

                            string rawResult = await HandleSubagentDispatchAsync(toolCall.Function.Arguments, workingDirectory);
                            string result = _contextGuard.Guard(rawResult, ContextConstants.MaxToolOutputTokens);
                            _conversationManager.AddMessage(new Message("tool", result, toolCall.Id));
                        }
                        else
                        {
                            ToolResult rawResult = _toolManager.ExecuteTool(toolCall.Function.Name, toolCall.Function.Arguments, workingDirectory, permissions);
                            string resultOutput = _contextGuard.Guard(rawResult.Output, ContextConstants.MaxToolOutputTokens);
                            _conversationManager.AddMessage(new Message("tool", resultOutput, toolCall.Id));
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(aiMessage.Content))
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
                
                // Add a summary of the conversation so far to the subagent
                string conversationSummary = string.Join("\n", _conversationManager.GetMessages()
                    .Select(m => $"[{m.Role}] {m.Content}"));
                contextBuilder.AppendLine(conversationSummary);

                _conversationManager.AddMessage(new Message("system", $"Context for subagent: {args.Name}\n{contextBuilder}"));
                
                // We don't use a full loop for subagents here for simplicity in this PoC, 
                // but we'll call the LLM.
                var response = await _llmClient.GetChatCompletionAsync(subagentHistory);
                return response?.Choices?.FirstOrDefault()?.Message.Content ?? "Subagent failed to return a result.";
            }
            catch (Exception ex)
            {
                return $"Error dispatching subagent: {ex.Message}";
            }
        }
    }
}
