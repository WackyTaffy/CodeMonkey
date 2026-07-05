using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeMonkey.Core.Interfaces
{
    public interface IAgentExecutor
    {
        ILLMClient Client { get; }
        Task<string> ExecuteLoopAsync(
            string agentLabel, 
            IConversationManager conversationManager, 
            string workingDirectory, 
            List<string>? permissions, 
            Action<string> onStatusUpdate, 
            Action<ToolResult> onToolExecuted, 
            string systemPrompt);
    }
}
