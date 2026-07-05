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
        private const int TotalTokenLimit = 15000;

        public Action<string>? OnStatusUpdate { get; set; }
        public Action<ToolResult>? OnToolExecuted { get; set; }
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

### 4. PRAGMATISM & SCOPE CONTROL
- SURGICAL FIRST: Prioritize small, targeted fixes over large architectural changes.
- AVOID SCOPE CREEP: Do not suggest 'improvements' or 'refactoring' unless explicitly asked or necessary for the fix.
- MINIMALISM: Write the least amount of code necessary to solve the problem.
";
        }

        public string GetSubagentSystemPrompt(string name, string task, string workingDirectory)
        {
            return $@"You are a specialized worker agent named '{name}'. Your sole purpose is to execute the following task: {task}.
You are working in '{workingDirectory}'.

### BEHAVIORAL CONSTRAINTS
- NO PROPOSALS: Do not propose plans or ask for human approval.
- NO CHECKPOINTS: Do not stop for human checkpoints.
- NO ORCHESTRATION: You are a worker, not an orchestrator. Do not dispatch further agents or manage a multi-stage project.
- ATOMICITY: Execute your task to completion and return the final result.
- CONCISE OUTPUT: Provide the result of your work clearly and concisely.";
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
            _conversationManager.AddMessage(new Message("user", userInput));
            return await ExecuteAgentLoopAsync("Main Agent", _conversationManager, workingDirectory, null);
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

        private async Task<string> ExecuteAgentLoopAsync(string agentLabel, IConversationManager? conversationManager, string workingDirectory, List<string>? permissions)
        {
            int iterations = 0;

            if(conversationManager == null)
            {
                conversationManager = new ConversationManager();
            }

            while (true)
            {
                iterations++;
                OnStatusUpdate?.Invoke($"[{agentLabel}] Iteration {iterations}: Thinking...");
                
                List<Message> currentMessages = conversationManager!.GetMessages().ToList();

                if (Verbose)
                {
                    int currentTokens = conversationManager!.GetTotalTokenCount();
                    OnStatusUpdate?.Invoke($"[{agentLabel}] [VERBOSE] Current Context Window: {currentTokens} tokens");
                    OnStatusUpdate?.Invoke($"[{agentLabel}] [VERBOSE] Message count: {currentMessages.Count}");
                }

                var response = await GetResponseWithRetryAsync(currentMessages, agentLabel);

                if (response == null || response.Choices == null || response.Choices.Count == 0)
                {
                    return $"AI Response was null or contained no Choices after multiple retries for {agentLabel}.";
                }

                var aiMessage = response.Choices[0].Message;

                // Output reasoning content if present
                if (!string.IsNullOrWhiteSpace(aiMessage?.ReasoningContent))
                {
                    OnStatusUpdate?.Invoke($"[{agentLabel}] REASONING: {aiMessage.ReasoningContent}");
                }

                if (aiMessage?.ToolCalls != null && aiMessage.ToolCalls.Count > 0)
                {
                    // If there is content AND tool calls, the content is likely thinking/reasoning
                    if (!string.IsNullOrWhiteSpace(aiMessage.Content))
                    {
                        OnStatusUpdate?.Invoke($"[{agentLabel}] THINKING: {aiMessage.Content}");
                    }

                    conversationManager!.AddMessage(aiMessage);

                    foreach (var toolCall in aiMessage.ToolCalls)
                    {
                        if (conversationManager!.ShouldCompact(TokenLimit))
                        {
                            OnStatusUpdate?.Invoke($"[{agentLabel}] Context limit reached ({conversationManager.GetTotalTokenCount()}/{TokenLimit}). Compacting context...");
                            await CompactContextAsync(workingDirectory);
                        }

                        OnStatusUpdate?.Invoke($"[{agentLabel}] Calling tool: {toolCall.Function.Name} with args: {toolCall.Function.Arguments}");
                        
                        if (toolCall.Function.Name == "dispatch_subagent")
                        {
                            if (permissions != null)
                            {
                                var errMsg = "Error: Subagents cannot dispatch further subagents.";
                                conversationManager!.AddMessage(new Message("tool", errMsg, toolCall.Id));
                                continue;
                            }

                            string result = await HandleSubagentDispatchAsync(toolCall.Function.Arguments, workingDirectory);
                            conversationManager!.AddMessage(new Message("tool", result, toolCall.Id));
                        }
                        else
                        {
                            ToolResult toolResult = _toolManager.ExecuteTool(toolCall.Function.Name, toolCall.Function.Arguments, workingDirectory, permissions);
                            
                            // Trigger event for UI transparency
                            OnToolExecuted?.Invoke(toolResult);
                            
                            conversationManager!.AddMessage(new Message("tool", toolResult.Result, toolCall.Id));
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(aiMessage?.Content))
                {
                    conversationManager!.AddMessage(aiMessage);
                    return aiMessage.Content;
                }
                else
                {
                    if (Verbose) OnStatusUpdate?.Invoke($"[{agentLabel}] [VERBOSE] AI returned an empty response with no tool calls");
                    return $"AI returned an empty response for {agentLabel}.";
                }

                if (conversationManager!.ShouldCompact(TokenLimit))
                {
                    OnStatusUpdate?.Invoke($"[{agentLabel}] Context limit reached ({conversationManager.GetTotalTokenCount()}/{TokenLimit}). Compacting context...");
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

                var subagentConvoMgr = new ConversationManager();
                string subagentSysPrompt = GetSubagentSystemPrompt(args.Name, args.Task, workingDirectory);
                subagentConvoMgr.AddMessage(new Message("system", subagentSysPrompt));

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
                subagentConvoMgr.AddMessage(new Message("context", contextBuilder.ToString()));

                subagentConvoMgr.AddMessage(new Message("user", args.Task));

                return await ExecuteAgentLoopAsync("Subagent: " + args.Name, subagentConvoMgr, workingDirectory, args.Permissions);
            }
            catch (Exception ex)
            {
                return $"Error dispatching subagent: {ex.Message}";
            }
        }
    }
}
