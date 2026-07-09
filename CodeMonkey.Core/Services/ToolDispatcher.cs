using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;

namespace CodeMonkey.Core.Services
{
    public class ToolDispatcher : IToolDispatcher
    {
        private readonly IToolManager _toolManager;
        private readonly ISubagentManager _subagentManager;

        public ToolDispatcher(IToolManager toolManager, ISubagentManager subagentManager)
        {
            _toolManager = toolManager;
            _subagentManager = subagentManager;
        }

        public async Task<ToolResult> DispatchToolAsync(
            string toolName, 
            string arguments, 
            string workingDirectory, 
            List<string>? permissions,
            IConversationManager conversationManager)
        {
            if (toolName == "dispatch_subagent")
            {
                return await _subagentManager.HandleSubagentDispatchAsync(arguments, workingDirectory);
            }

            ToolResult toolResult = _toolManager.ExecuteTool(toolName, arguments, workingDirectory, permissions);
            return toolResult;
        }
    }
}
