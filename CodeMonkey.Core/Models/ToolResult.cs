using System;

namespace CodeMonkey.Core.Models
{
    public class ToolResult
    {
        public string Output { get; set; } = string.Empty;
        public bool ApprovalRequired { get; set; }
        public Guid? ManifestId { get; set; }

        public ToolResult(string output)
        {
            Output = output;
            ApprovalRequired = false;
        }

        public ToolResult(Guid manifestId)
        {
            Output = "Approval required.";
            ApprovalRequired = true;
            ManifestId = manifestId;
        }

        public static ToolResult Success(string output) => new ToolResult(output);
        public static ToolResult NeedsApproval(Guid manifestId) => new ToolResult(manifestId);
    }
}
