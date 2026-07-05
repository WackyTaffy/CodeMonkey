using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public async Task<string> DispatchToolAsync(
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

            var toolResult = _toolManager.ExecuteTool(toolName, arguments, workingDirectory, permissions);
            return toolResult.Result;
        }
    }
}
