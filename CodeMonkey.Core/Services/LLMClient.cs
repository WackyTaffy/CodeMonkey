using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeMonkey.Core.Services
{
    public class LLMClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly IToolManager _toolManager;
        private const string ApiUrl = "http://localhost:8080/v1/chat/completions";

        public LLMClient(HttpClient httpClient, IToolManager toolManager)
        {
            _httpClient = httpClient;
            _toolManager = toolManager;
        }

        public async Task<ChatResponse> GetChatCompletionAsync(List<Message> messages, bool isSubagent = false)
        {
            var requestBody = new
            {
                model = "gemma",
                messages = messages,
                tools = _toolManager.GetToolDefinitions(isSubagent),
                tool_choice = "auto"
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var response = await _httpClient.PostAsync(ApiUrl, new StringContent(jsonContent, Encoding.UTF8, "application/json"));
            var resultString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"LLM API request failed with status code {response.StatusCode}. Response: {resultString}");
            }

            try
            {
                ChatResponse retVal = JsonSerializer.Deserialize<ChatResponse>(resultString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;

                retVal.TokenUsageStats = ExtractTokenUsageDynamic(resultString);
                return retVal;
            }
            catch (JsonException ex)
            {
                throw new JsonException($"Failed to deserialize LLM response. Response body: {resultString}", ex);
            }
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
