using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace CodeMonkey.Core.Models
{
    public class ChatResponse
    {
        [JsonPropertyName("choices")]
        [YamlMember(Alias = "choices")]
        public List<Choice> Choices { get; set; }

        public Dictionary<string, int> TokenUsageStats { get; set; } = new();

        public override string ToString() =>
            "\tTOKEN USAGE:\n\t\t" + string.Join(", ", TokenUsageStats.Select(kvp => $"{kvp.Key} = {kvp.Value}")) + "\n" +
            "\tCHOICES:\n" +
            string.Join("\n", (Choices ?? []).Select(x=>$"\t\t{x}"));
    }
}
