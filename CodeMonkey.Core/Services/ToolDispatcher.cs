using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using CodeMonkey.Core.Utility;

namespace CodeMonkey.Core.Services
{
    public class ToolDispatcher : IToolDispatcher
    {
        private readonly IToolManager _toolManager;
        private readonly ISubagentManager _subagentManager;
        private readonly ITokenHelper _tokenHelper;

        private const int _TRUNCATION_LIMIT = 4000;

        public ToolDispatcher(IToolManager toolManager, ISubagentManager subagentManager, ITokenHelper tokenHelper)
        {
            _toolManager = toolManager;
            _subagentManager = subagentManager;
            _tokenHelper = tokenHelper;
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

            if (_tokenHelper.GetTokenCount(toolResult.Result) > _TRUNCATION_LIMIT)
                toolResult.Result = $"The tool result was too large (over {_TRUNCATION_LIMIT} tokens) and was truncated. " +
                    $"If the full contents is needed, try again and write the contents to a file for surgical reads/extraction." +
                    $"\n[TRUNCATED CONTENT START]" +
                    $"\n{toolResult.Result.Substring(0, _TRUNCATION_LIMIT)}" +
                    $"\n[TRUNCATED CONTENT END]";

            return toolResult;
        }
    }
}
