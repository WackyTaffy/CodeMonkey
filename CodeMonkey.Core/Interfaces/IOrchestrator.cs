using CodeMonkey.Core.Models;

namespace CodeMonkey.Core.Interfaces
{
    public interface IOrchestrator
    {
        Action<string>? OnStatusUpdate { get; set; }
        Func<Guid, string, Task<bool>>? OnApprovalRequired { get; set; }
        bool Verbose { get; set; }
        Task<string> ProcessUserRequestAsync(string userInput, string workingDirectory);
        Task<string> CompactContextAsync(string workingDirectory);
        void BootstrapContext(string workingDirectory);
        string GetSystemPrompt(string workingDirectory);
    }
}
