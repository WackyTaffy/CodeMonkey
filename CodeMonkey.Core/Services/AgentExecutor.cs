using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeMonkey.Core.Services
{
    public class AgentExecutor : IAgentExecutor
    {
        private readonly ILLMClient _llmClient;
        private readonly IToolDispatcher _toolDispatcher;
        private readonly IConversationManager _conversationManager; // This is for the global state, but we use the one passed to ExecuteLoopAsync

        public ILLMClient Client => _llmClient;

        public AgentExecutor(ILLMClient llmClient, IToolDispatcher toolDispatcher, IConversationManager conversationManager)
        {
            _llmClient = llmClient;
            _toolDispatcher = toolDispatcher;
            _conversationManager = conversationManager;
        }

        public async Task<string> ExecuteLoopAsync(
            string agentLabel, 
            IConversationManager conversationManager, 
            string workingDirectory, 
            List<string>? permissions, 
            Action<string> onStatusUpdate, 
            Action<ToolResult> onToolExecuted, 
            string systemPrompt)
        {
            int iterations = 0;
            const int TokenLimit = 12500;

            while (true)
            {
                iterations++;
                onStatusUpdate($"[{agentLabel}] Iteration {iterations}: Thinking...");

                List<Message> currentMessages = conversationManager.GetMessages().ToList();

                var response = await GetResponseWithRetryAsync(currentMessages, agentLabel, onStatusUpdate);

                if (response == null || response.Choices == null || response.Choices.Count == 0)
                {
                    return $"AI Response was null or contained no Choices after multiple retries for {agentLabel}.";
                }

                var aiMessage = response.Choices[0].Message;

                if (!string.IsNullOrWhiteSpace(aiMessage?.ReasoningContent))
                {
                    onStatusUpdate($"[{agentLabel}] REASONING: {aiMessage.ReasoningContent}");
                }

                if (aiMessage?.ToolCalls != null && aiMessage.ToolCalls.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(aiMessage.Content))
                    {
                        onStatusUpdate($"[{agentLabel}] THINKING: {aiMessage.Content}");
                    }

                    conversationManager.AddMessage(aiMessage);

                    foreach (var toolCall in aiMessage.ToolCalls)
                    {
                        if (conversationManager.ShouldCompact(TokenLimit))
                        {
                            onStatusUpdate($"[{agentLabel}] Context limit reached ({conversationManager.GetTotalTokenCount()}/{TokenLimit}). Compacting context...");
                            await CompactContextAsync(conversationManager, workingDirectory, systemPrompt);
                        }

                        onStatusUpdate($"[{agentLabel}] Calling tool: {toolCall.Function.Name} with args: {toolCall.Function.Arguments}");

                        string result = await _toolDispatcher.DispatchToolAsync(
                            toolCall.Function.Name, 
                            toolCall.Function.Arguments, 
                            workingDirectory, 
                            permissions, 
                            conversationManager);
                        
                        onToolExecuted(new ToolResult { Result = result, ToolName = toolCall.Function.Name });
                        conversationManager.AddMessage(new Message("tool", result, toolCall.Id));
                    }
                }
                else if (!string.IsNullOrWhiteSpace(aiMessage?.Content))
                {
                    conversationManager.AddMessage(aiMessage);
                    return aiMessage.Content;
                }
                else
                {
                    return $"AI returned an empty response for {agentLabel}.";
                }

                if (conversationManager.ShouldCompact(TokenLimit))
                {
                    onStatusUpdate($"[{agentLabel}] Context limit reached ({conversationManager.GetTotalTokenCount()}/{TokenLimit}). Compacting context...");
                    await CompactContextAsync(conversationManager, workingDirectory, systemPrompt);
                }
            }
        }

        private async Task<string> CompactContextAsync(IConversationManager conversationManager, string workingDirectory, string systemPrompt)
        {
            return await conversationManager.CompactAsync(_llmClient, systemPrompt);
        }

        private async Task<ChatResponse?> GetResponseWithRetryAsync(List<Message> messages, string agentLabel, Action<string> onStatusUpdate, int maxRetries = 3)
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
                    onStatusUpdate($"[{agentLabel}] [VERBOSE] AI response was empty or null. Retry {i + 1}/{maxRetries}...");
                }
                catch (Exception ex)
                {
                    onStatusUpdate($"[{agentLabel}] [VERBOSE] LLM call failed: {ex.Message}. Retry {i + 1}/{maxRetries}...");
                }

                if (i < maxRetries - 1)
                {
                    await Task.Delay(1000 * (i + 1));
                }
            }
            return null;
        }
    }
}
