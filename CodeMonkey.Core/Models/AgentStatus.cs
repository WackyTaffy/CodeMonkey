using System.Diagnostics.CodeAnalysis;

namespace CodeMonkey.Core.Models
{
    public class AgentStatus
    {
        public required string StatusMessage { get; set; }

        public required int ContextSize { get; set; }

        public required bool IsSubagent { get; set; }


        [SetsRequiredMembers]
        public AgentStatus(string statusMessage, int contextSize, bool isSubagent)
        {
            StatusMessage = statusMessage;
            ContextSize = contextSize;
            IsSubagent = isSubagent;
        }

        public override string ToString()
        {
            string indent = IsSubagent ? "\t\t" : "";
            return $"{indent}{{ {ContextSize} }} {StatusMessage}";
        }
    }
}
