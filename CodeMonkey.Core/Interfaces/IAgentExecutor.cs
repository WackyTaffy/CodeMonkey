using CodeMonkey.Core.Models;

namespace CodeMonkey.Core.Interfaces
{
    public interface IAgentExecutor
    {
        ILLMClient Client { get; }
        Task<ToolResult> ExecuteLoopAsync(
            string agentLabel, 
            IConversationManager conversationManager, 
            string workingDirectory, 
            Action<AgentStatus> onStatusUpdate, 
            Action<ToolResult> onToolExecuted, 
            string systemPrompt,
            bool isSubagent = false);
    }
}
