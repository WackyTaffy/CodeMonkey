using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using CodeMonkey.Core.Utility;
using System.Text;

namespace CodeMonkey.Core.Services
{
    public class ConversationManager : IConversationManager
    {
        private readonly List<Message> _messages = new();
        private readonly GemmaTokenHelper _tokenHelper = new();

        public IEnumerable<Message> GetMessages() => _messages;

        public void AddMessage(Message message)
        {
            _messages.Add(message);
        }

        public int GetTotalTokenCount() => GetTotalTokenCount(_tokenHelper, _messages);

        public static int GetTotalTokenCount(GemmaTokenHelper tokenHelper, List<Message> messages)
        {
            int totalTokens = 0;
            foreach (var msg in messages)
            {
                totalTokens += tokenHelper.GetTokenCount(msg.Role);
                totalTokens += tokenHelper.GetTokenCount(msg.Content ?? "");
                if (msg.ToolCalls != null)
                {
                    foreach (var call in msg.ToolCalls)
                    {
                        totalTokens += tokenHelper.GetTokenCount(call.Function.Name);
                        totalTokens += tokenHelper.GetTokenCount(call.Function.Arguments);
                    }
                }
                if (!string.IsNullOrEmpty(msg.ToolCallId))
                {
                    totalTokens += tokenHelper.GetTokenCount(msg.ToolCallId);
                }
            }
            return totalTokens;
        }

        public bool ShouldCompact(int tokenLimit)
        {
            return GetTotalTokenCount() >= tokenLimit;
        }

        public async Task<string> CompactAsync(ILLMClient llmClient, string systemPrompt)
        {
            // We need at least: System prompt, one user msg, one AI msg to make sense of a "round"
            if (_messages.Count < 4) return "FAILED: Message count under threshhold for compaction";

            // 1. Preserve the essentials
            var systemPromptMsg = _messages.FirstOrDefault(m => m.Role == "system") ?? Message.AsSystemPrompt(systemPrompt);
            
            // Preserve the last round (last two messages)
            var lastRound = _messages.Skip(_messages.Count - 2).ToList();

            // 2. Identify messages to summarize (exclude system prompt and last round)
            var messagesToSummarize = _messages
                .Where((msg, index) => index != 0 && index < _messages.Count - 2)
                .ToList();

            if (!messagesToSummarize.Any()) return "FAILED: No messages avaliablie for compaction";

            var summaryPrompt = new StringBuilder();
            summaryPrompt.AppendLine("Summarize the following conversation history. ");
            summaryPrompt.AppendLine("Focus on: current objectives, key decisions made, and the current state of the project.");
            summaryPrompt.AppendLine("CRITICAL: List up to 5 most useful relative file paths mentioned or worked on in this history.");
            summaryPrompt.AppendLine("Be concise and maintain a technical tone.");
            summaryPrompt.AppendLine("\n--- HISTORY TO SUMMARIZE ---");

            foreach (var msg in messagesToSummarize)
            {
                summaryPrompt.AppendLine($"[{msg.Role}]: {msg.Content}");
            }

            var compactionMessages = new List<Message>
            {
                Message.AsSystemPrompt("You are a context compaction assistant. Your job is to summarize long conversations into a concise state summary for another AI."),
                Message.AsUserMessage(summaryPrompt.ToString())
            };

            var response = await llmClient.GetChatCompletionAsync(compactionMessages);
            string summary = response?.Choices?.FirstOrDefault()?.Message?.Content ?? "No summary generated.";

            // 3. Reset and Re-build history
            _messages.Clear();
            _messages.Add(systemPromptMsg);
            _messages.Add(Message.AsSystemPrompt($"Previous session summary: {summary}"));
            _messages.AddRange(lastRound);

            return summary;
        }
    }
}
