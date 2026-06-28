using CodeMonkey.Core.Models;

namespace CodeMonkey.Core.Interfaces
{
    public interface IConversationManager
    {
        IEnumerable<Message> GetMessages();
        void AddMessage(Message message);
        bool ShouldCompact(int tokenLimit);
        Task<string> CompactAsync(ILLMClient llmClient, string systemPrompt);
        int GetTotalTokenCount();
    }
}
