using CodeMonkey.Core.Models;

namespace CodeMonkey.Core.Interfaces
{
    public interface ILLMClient
    {
        Task<ChatResponse> GetChatCompletionAsync(List<Message> messages, bool isSubagent = false);
    }
}
