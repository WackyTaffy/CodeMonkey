using System.Threading.Tasks;
using CodeMonkey.Core.Models;

namespace CodeMonkey.Core.Interfaces
{
    public interface IToolDispatcher
    {
        Task<string> DispatchToolAsync(
            string toolName, 
            string arguments, 
            string workingDirectory, 
            List<string>? permissions,
            IConversationManager conversationManager);
    }
}
