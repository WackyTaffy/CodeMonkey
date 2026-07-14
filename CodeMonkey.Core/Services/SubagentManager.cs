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

        public Action<AgentStatus>? OnStatusUpdate { get; set; }
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

        public async Task<ToolResult> HandleSubagentDispatchAsync(string args, string workingDirectory)
        {
            if (_agentExecutor == null) return ToolResult.Error("dispatch_subagent", "SubagentManager not initialized with executor");

            try
            {
                var argsParsed = _toolManager.ParseArguments<SubagentDispatchArgs>(args);
                if (argsParsed == null) return ToolResult.Error("dispatch_subagent", "Invalid arguments for subagent dispatch");

                var subagentConvoMgr = new ConversationManager();
                string subagentSysPrompt = _promptProvider.GetSubagentSystemPrompt(argsParsed.Name, argsParsed.Task, workingDirectory);
                subagentConvoMgr.AddMessage(Message.AsSystemPrompt(subagentSysPrompt));

                var contextBuilder = new StringBuilder();
                contextBuilder.AppendLine("--- INITIAL CONTEXT ---");
                contextBuilder.AppendLine($"Task: {argsParsed.Task}");
                contextBuilder.AppendLine("\nRelevant Files:");
                foreach (var filePath in argsParsed.InitialContext)
                {
                    string content = _fileSystem.ReadFile(filePath, workingDirectory);
                    contextBuilder.AppendLine($"\nFile: {filePath}\nContent:\n{content}\n---");
                }
                contextBuilder.AppendLine("\n--- END INITIAL CONTEXT ---");
                subagentConvoMgr.AddMessage(Message.AsSystemPrompt(contextBuilder.ToString()));

                subagentConvoMgr.AddMessage(Message.AsUserMessage(argsParsed.Task));

                return await _agentExecutor.ExecuteLoopAsync(
                    "Subagent: " + argsParsed.Name, 
                    subagentConvoMgr, 
                    workingDirectory,
                    (status) => OnStatusUpdate?.Invoke(status),
                    (toolResult) => OnToolExecuted?.Invoke(toolResult),
                    subagentSysPrompt,
                    isSubagent: true);
            }
            catch (Exception ex)
            {
                return ToolResult.Error("dispatch_subagent", ex);
            }
        }
    }
}
