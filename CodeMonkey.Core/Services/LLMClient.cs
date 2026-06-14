using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System.Text;
using System.Text.Json;
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

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var content = serializer.Serialize(requestBody);
            var response = await _httpClient.PostAsync(ApiUrl, new StringContent(content, Encoding.UTF8, "application/x-yaml"));
            var resultString = await response.Content.ReadAsStringAsync();

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            try 
            {
                return deserializer.Deserialize<ChatResponse>(resultString);
            }
            catch (Exception)
            {
                // Fallback to JSON if YAML deserialization fails
                return JsonSerializer.Deserialize<ChatResponse>(resultString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }

        public List<object> GetToolDefinitions()
        {
            return new List<object>
            {
                new {
                    type = "function",
                    function = new {
                        name = "write_file",
                        description = "Writes content to a file at the specified path.",
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
                        description = "Reads the content of a file.",
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
                        name = "run_command",
                        description = "Runs a shell command (e.g., 'dotnet build').",
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
    }
}
