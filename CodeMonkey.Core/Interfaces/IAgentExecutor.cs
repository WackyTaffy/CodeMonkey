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
            List<string>? permissions, 
            Action<string> onStatusUpdate, 
            Action<ToolResult> onToolExecuted, 
            string systemPrompt);
    }
}
