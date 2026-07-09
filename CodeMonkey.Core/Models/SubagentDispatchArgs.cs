using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace CodeMonkey.Core.Models
{
    public class SubagentDispatchArgs
    {
        [YamlMember(Alias = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Subagent";

        [YamlMember(Alias = "task")]
        [JsonPropertyName("task")]
        public string Task { get; set; } = string.Empty;

        [YamlMember(Alias = "permissions")]
        [JsonPropertyName("permissions")]
        public List<string> Permissions { get; set; } = new List<string>();

        [YamlMember(Alias = "initial_context")]
        [JsonPropertyName("initial_context")]
        public List<string> InitialContext { get; set; } = new List<string>();
    }
}
