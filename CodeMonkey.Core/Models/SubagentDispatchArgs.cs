namespace CodeMonkey.Core.Models
{
    public class SubagentDispatchArgs
    {
        public string Task { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
        public List<string> InitialContext { get; set; } = new List<string>();
    }
}
