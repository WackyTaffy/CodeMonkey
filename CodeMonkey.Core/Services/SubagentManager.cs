using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System.Text;

namespace CodeMonkey.Core.Services
{
    public class SubagentManager : ISubagentManager
    {
        private IAgentExecutor? _agentExecutor; // Set via property
        private readonly IPromptProvider _promptProvider;
        private readonly IFileSystem _fileSystem;
        private readonly IToolManager _toolManager;

        public Action<string>? OnStatusUpdate { get; set; }
        public Action<ToolResult>? OnToolExecuted { get; set; }

        public SubagentManager(IPromptProvider promptProvider, IFileSystem fileSystem, IToolManager toolManager)
        {
            _promptProvider = promptProvider;
            _fileSystem = fileSystem;
            _toolManager = toolManager;
        }

        public void SetExecutor(IAgentExecutor executor)
        {
            _agentExecutor = executor;
        }

        public async Task<ToolResult> HandleSubagentDispatchAsync(string argsYaml, string workingDirectory)
        {
            if (_agentExecutor == null) return ToolResult.Error("dispatch_subagent", "SubagentManager not initialized with executor");

            try
            {
                var args = _toolManager.ParseArguments<SubagentDispatchArgs>(argsYaml);
                if (args == null) return ToolResult.Error("dispatch_subagent", "Invalid arguments for subagent dispatch");

                var subagentConvoMgr = new ConversationManager();
                string subagentSysPrompt = _promptProvider.GetSubagentSystemPrompt(args.Name, args.Task, workingDirectory);
                subagentConvoMgr.AddMessage(Message.AsSystemPrompt(subagentSysPrompt));

                var contextBuilder = new StringBuilder();
                contextBuilder.AppendLine("--- INITIAL CONTEXT ---");
                contextBuilder.AppendLine($"Task: {args.Task}");
                contextBuilder.AppendLine("\nRelevant Files:");
                foreach (var filePath in args.InitialContext)
                {
                    string content = _fileSystem.ReadFile(filePath, workingDirectory);
                    contextBuilder.AppendLine($"\nFile: {filePath}\nContent:\n{content}\n---");
                }
                contextBuilder.AppendLine("\n--- END INITIAL CONTEXT ---");
                subagentConvoMgr.AddMessage(Message.AsSystemPrompt(contextBuilder.ToString()));

                subagentConvoMgr.AddMessage(Message.AsUserMessage(args.Task));

                return await _agentExecutor.ExecuteLoopAsync(
                    "Subagent: " + args.Name, 
                    subagentConvoMgr, 
                    workingDirectory, 
                    args.Permissions,
                    (status) => OnStatusUpdate?.Invoke(status),
                    (toolResult) => OnToolExecuted?.Invoke(toolResult),
                    subagentSysPrompt);
            }
            catch (Exception ex)
            {
                return ToolResult.Error("dispatch_subagent", ex);
            }
        }
    }
}
