using System.Text.Json;
using CodeMonkey.Core.Models;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class ModelMappingTests
    {
        [Test]
        public void ReasoningContent_ShouldBePopulated_WhenPresentInJson()
        {
            // Arrange
            string json = @"{
                ""choices"": [
                    {
                        ""message"": {
                            ""role"": ""assistant"",
                            ""content"": ""Hello!"",
                            ""reasoning_content"": ""I am thinking about the greeting.""
                        }
                    }
                ]
            }";

            // Act
            var response = JsonSerializer.Deserialize<ChatResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            Assert.That(response?.Choices, Is.Not.Null);
            Assert.That(response?.Choices, Has.Count.EqualTo(1));
            Assert.That(response?.Choices[0].Message.ReasoningContent, Is.EqualTo("I am thinking about the greeting."));
            Assert.That(response?.Choices[0].Message.Content, Is.EqualTo("Hello!"));
        }

        [Test]
        public void ReasoningContent_ShouldBeNull_WhenAbsentInJson()
        {
            // Arrange
            string json = @"{
                ""choices"": [
                    {
                        ""message"": {
                            ""role"": ""assistant"",
                            ""content"": ""Hello!""
                        }
                    }
                ]
            }";

            // Act
            var response = JsonSerializer.Deserialize<ChatResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            Assert.That(response?.Choices, Is.Not.Null);
            Assert.That(response?.Choices[0].Message.ReasoningContent, Is.Null);
        }

        [Test]
        public void MessageToString_ShouldIncludeReasoningLength()
        {
            // Arrange
            var message = new Message
            {
                Role = "assistant",
                Content = "Hello",
                ReasoningContent = "Thinking..."
            };

            // Act
            var result = message.ToString();

            // Assert
            Assert.That(result, Does.Contain("Reasoning Length = 11"));
            Assert.That(result, Does.Contain("Content Length = 5"));
        }
    }
}
