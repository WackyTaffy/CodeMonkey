using CodeMonkey.Core.Models;

namespace CodeMonkey.Core.Services
{
    public interface IOrchestrator
    {
        Task<string> ProcessUserRequestAsync(string userInput, string workingDirectory, List<Message> history);
        Task<string> CompactContextAsync(List<Message> history, string workingDirectory);
        void BootstrapContext(List<Message> history, string workingDirectory);
    }
}
