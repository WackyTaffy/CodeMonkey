using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace CodeMonkey.Core.Models
{
    public class FunctionCall
    {
        [JsonPropertyName("name")]
        [YamlMember(Alias = "name")]
        public required string Name { get; set; }

        [JsonPropertyName("arguments")]
        [YamlMember(Alias = "arguments")]
        public required string Arguments { get; set; }
    }
}
