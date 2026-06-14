using CodeMonkey.Core.Models;
using CodeMonkey.Core.Interfaces;
using System.Text;
using System.Text.Json;

namespace CodeMonkey.Core.Interfaces
{
    public interface ILLMClient
    {
        Task<ChatResponse> GetChatCompletionAsync(List<Message> messages);
        List<object> GetToolDefinitions();
    }
}
