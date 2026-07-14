using CodeMonkey.Core.Models;

namespace CodeMonkey.Core.Interfaces
{
    public interface IOrchestrator
    {
        Action<AgentStatus>? OnStatusUpdate { get; set; }
        Action<ToolResult>? OnToolExecuted { get; set; }
        bool Verbose { get; set; }
        Task<ToolResult> ProcessUserRequestAsync(string userInput, string workingDirectory);
        Task<string> CompactContextAsync(string workingDirectory);
        void BootstrapContext(string workingDirectory);
        string GetSystemPrompt(string workingDirectory);
    }
}
