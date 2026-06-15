using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CodeMonkey.Core.Services
{
    public class LLMClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "http://localhost:8080/v1/chat/completions";

        public LLMClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ChatResponse> GetChatCompletionAsync(List<Message> messages)
        {
            var requestBody = new
            {
                model = "gemma",
                messages = messages,
                tools = GetToolDefinitions(),
                tool_choice = "auto"
            };

            //var serializer = new SerializerBuilder()
            //    .WithNamingConvention(CamelCaseNamingConvention.Instance)
            //    .Build();
            //var content = serializer.Serialize(requestBody);

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var response = await _httpClient.PostAsync(ApiUrl, new StringContent(jsonContent, Encoding.UTF8, "application/x-yaml"));
            var resultString = await response.Content.ReadAsStringAsync();

            ChatResponse retVal = JsonSerializer.Deserialize<ChatResponse>(resultString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;

            retVal.TokenUsageStats = ExtractTokenUsageDynamic(resultString);

            return retVal;

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return deserializer.Deserialize<ChatResponse>(resultString);
        }

        public string GetToolDefinitionsYaml()
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return serializer.Serialize(GetToolDefinitions());
        }

        public List<object> GetToolDefinitions()
        {
            return new List<object>
            {
                new {
                    type = "function",
                    function = new {
                        name = "write_file",
                        description = "Writes content to a file at the specified path",
                        parameters = new {
                            type = "object",
                            properties = new {
                                path = new { type = "string", description = "The file path" },
                                content = new { type = "string", description = "The text content to write" }
                            },
                            required = new[] { "path", "content" }
                        }
                    }
                },
                new {
                    type = "function",
                    function = new {
                        name = "read_file",
                        description = "Reads the content of a file",
                        parameters = new {
                            type = "object",
                            properties = new {
                                path = new { type = "string", description = "The file path" }
                            },
                            required = new[] { "path" }
                        }
                    }
                },
                new {
                    type = "function",
                    function = new {
                        name = "get_file_list",
                        description = "Gets a list of accessible files in the directory as relative paths",
                        parameters = new {
                            type = "object",
                            properties = new {
                                recursive = new { type = "bool", description = "The file list will contain files in subdirectories" },
                                searchPattern = new { type = "string", description = "The search string to match against the names of files in path. " +
                                    "This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions" }
                            }
                        }
                    }
                },
                new {
                    type = "function",
                    function = new {
                        name = "run_command",
                        description = "Runs a shell command (e.g., 'dotnet build')",
                        parameters = new {
                            type = "object",
                            properties = new {
                                command = new { type = "string", description = "The shell command to execute" }
                            },
                            required = new[] { "command" }
                        }
                    }
                }
            };
        }


        public static Dictionary<string, int> ExtractTokenUsageDynamic(string json)
        {
            var result = new Dictionary<string, int>();
            var doc = JsonNode.Parse(json);

            if (doc?["usage"] is JsonObject usage)
            {
                if (usage["completion_tokens"]?.GetValue<int>() is int ct)
                    result["completion_tokens"] = ct;
                if (usage["prompt_tokens"]?.GetValue<int>() is int pt)
                    result["prompt_tokens"] = pt;
                if (usage["total_tokens"]?.GetValue<int>() is int tt)
                    result["total_tokens"] = tt;
                if (usage["prompt_tokens_details"]?["cached_tokens"]?.GetValue<int>() is int cached)
                    result["cached_tokens"] = cached;
            }

            return result;
        }

    }
}
