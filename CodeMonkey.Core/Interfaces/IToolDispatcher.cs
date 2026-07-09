using CodeMonkey.Core.Models;

namespace CodeMonkey.Core.Interfaces
{
    public interface IToolDispatcher
    {
        Task<ToolResult> DispatchToolAsync(
            string toolName, 
            string arguments, 
            string workingDirectory, 
            List<string>? permissions,
            IConversationManager conversationManager);
    }
}
