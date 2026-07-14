using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;

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

        public async Task<ToolResult> ExecuteLoopAsync(
            string agentLabel, 
            IConversationManager conversationManager, 
            string workingDirectory, 
            Action<AgentStatus> onStatusUpdate, 
            Action<ToolResult> onToolExecuted, 
            string systemPrompt,
            bool isSubagent = false)
        {
            const int TokenLimit = 12500;

            while (true)
            {
                List<Message> currentMessages = conversationManager.GetMessages().ToList();

                var response = await GetResponseWithRetryAsync(conversationManager, currentMessages, agentLabel, onStatusUpdate, isSubagent: isSubagent);

                if (response == null || response.Choices == null || response.Choices.Count == 0)
                {
                    return ToolResult.Error(agentLabel, $"AI Response was null or contained no Choices after multiple retries for {agentLabel}.");
                }

                var aiMessage = response.Choices[0].Message;

                if (!string.IsNullOrWhiteSpace(aiMessage?.ReasoningContent))
                {
                    UpdateStatus(conversationManager, onStatusUpdate, $"[{agentLabel}] REASONING: {aiMessage.ReasoningContent}", isSubagent);
                }

                if (aiMessage?.ToolCalls != null && aiMessage.ToolCalls.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(aiMessage.Content))
                    {
                        UpdateStatus(conversationManager, onStatusUpdate, $"[{agentLabel}] THINKING: {aiMessage.Content}", isSubagent);
                    }

                    conversationManager.AddMessage(aiMessage);

                    if(aiMessage.ToolCalls.Count(x => x.Function.Name == "") > 1)
                        return ToolResult.Error(agentLabel, $"Only one write command can be handled at once.");

                    foreach (var toolCall in aiMessage.ToolCalls)
                    {
                        if (conversationManager.ShouldCompact(TokenLimit))
                        {
                            UpdateStatus(conversationManager, onStatusUpdate, $"[{agentLabel}] Context limit reached ({conversationManager.GetTotalTokenCount()}/{TokenLimit}). Compacting context...", isSubagent);
                        }

                        ToolResult result = await _toolDispatcher.DispatchToolAsync(
                            toolCall.Function.Name,
                            toolCall.Function.Arguments,
                            workingDirectory,
                            conversationManager);

                        onToolExecuted(result);

                        var msg = Message.AsToolResult(toolCall.Id, result);
                        conversationManager.AddMessage(msg);

                        if (result.RequiresContextRefresh)
                        {
                            UpdateStatus(conversationManager, onStatusUpdate, $"[{agentLabel}] Structural change detected. Forcing context refresh...", isSubagent);
                            break;
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(aiMessage?.Content))
                {
                    conversationManager.AddMessage(aiMessage);
                    return ToolResult.Success(agentLabel, aiMessage.Content);
                }
                else
                {
                    return ToolResult.Error(agentLabel, $"AI returned an empty response for {agentLabel}.");
                }

                if (conversationManager.ShouldCompact(TokenLimit))
                {
                    UpdateStatus(conversationManager, onStatusUpdate, $"[{agentLabel}] Context limit reached ({conversationManager.GetTotalTokenCount()}/{TokenLimit}). Compacting context...", isSubagent);
                    await CompactContextAsync(conversationManager, workingDirectory, systemPrompt);
                }
            }
        }

        private async Task<string> CompactContextAsync(IConversationManager conversationManager, string workingDirectory, string systemPrompt)
        {
            return await conversationManager.CompactAsync(_llmClient, systemPrompt);
        }

        private async Task<ChatResponse?> GetResponseWithRetryAsync(IConversationManager conversationManager, List<Message> messages, string agentLabel, Action<AgentStatus> onStatusUpdate, int maxRetries = 3, bool isSubagent = false)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = await _llmClient.GetChatCompletionAsync(messages, isSubagent);
                    if (response?.Choices != null && response.Choices.Count > 0)
                    {
                        return response;
                    }
                    UpdateStatus(conversationManager, onStatusUpdate, $"[{agentLabel}] [VERBOSE] AI response was empty or null. Retry {i + 1}/{maxRetries}...", isSubagent);
                }
                catch (Exception ex)
                {
                    UpdateStatus(conversationManager, onStatusUpdate, $"[{agentLabel}] [VERBOSE] LLM call failed: {ex.Message}. Retry {i + 1}/{maxRetries}...", isSubagent);
                }

                if (i < maxRetries - 1)
                {
                    await Task.Delay(1000 * (i + 1));
                }
            }
            return null;
        }

        private void UpdateStatus(IConversationManager conversationManager, Action<AgentStatus> onStatusUpdate, string status, bool isSubagent)
        {
            int contextSize = conversationManager.GetTotalTokenCount();
            onStatusUpdate(new AgentStatus(status, contextSize, isSubagent));
        }
    }
}
