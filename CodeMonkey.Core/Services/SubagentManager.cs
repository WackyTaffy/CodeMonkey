using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CodeMonkey.Core.Services
{
    public class SubagentManager : ISubagentManager
    {
        private IAgentExecutor _agentExecutor; // Set via property
        private readonly IPromptProvider _promptProvider;
        private readonly IFileSystem _fileSystem;
        private readonly IToolManager _toolManager;

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

        public async Task<string> HandleSubagentDispatchAsync(string argsYaml, string workingDirectory)
        {
            if (_agentExecutor == null) return "Error: SubagentManager not initialized with executor";

            try
            {
                var args = _toolManager.ParseArguments<SubagentDispatchArgs>(argsYaml);
                if (args == null) return "Error: Invalid arguments for subagent dispatch";

                var subagentConvoMgr = new ConversationManager();
                string subagentSysPrompt = _promptProvider.GetSubagentSystemPrompt(args.Name, args.Task, workingDirectory);
                subagentConvoMgr.AddMessage(new Message("system", subagentSysPrompt));

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
                subagentConvoMgr.AddMessage(new Message("context", contextBuilder.ToString()));

                subagentConvoMgr.AddMessage(new Message("user", args.Task));

                return await _agentExecutor.ExecuteLoopAsync(
                    "Subagent: " + args.Name, 
                    subagentConvoMgr, 
                    workingDirectory, 
                    args.Permissions, 
                    (status) => { }, 
                    (result) => { },
                    subagentSysPrompt);
            }
            catch (Exception ex)
            {
                return $"Error dispatching subagent: {ex.Message}";
            }
        }
    }
}
