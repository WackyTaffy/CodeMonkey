using CodeMonkey.Core.Models;

namespace CodeMonkey.Core.Interfaces
{
    public interface ISubagentManager
    {
        void SetExecutor(IAgentExecutor executor);
        Task<ToolResult> HandleSubagentDispatchAsync(string argsYaml, string workingDirectory);
    }
}
