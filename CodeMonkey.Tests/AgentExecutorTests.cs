using NSubstitute;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using CodeMonkey.Core.Services;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class AgentExecutorTests
    {
        private ILLMClient _mockLlmClient;
        private IToolDispatcher _mockToolDispatcher;
        private IConversationManager _mockConversationManager;
        private AgentExecutor _agentExecutor;
        private const string WorkingDir = @"C:\temp";
        private const string SystemPrompt = "You are a helpful assistant.";

        [SetUp]
        public void Setup()
        {
            _mockLlmClient = Substitute.For<ILLMClient>();
            _mockToolDispatcher = Substitute.For<IToolDispatcher>();
            _mockConversationManager = Substitute.For<IConversationManager>();
            
            _agentExecutor = new AgentExecutor(
                _mockLlmClient, 
                _mockToolDispatcher, 
                _mockConversationManager);
        }

        [Test]
        public async Task ExecuteLoopAsync_SimpleResponse_ReturnsContent()
        {
            // Arrange
            string expectedContent = "The answer is 42.";
            var chatResponse = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice { Message = new Message("assistant", expectedContent) }
                }
            };

            _mockLlmClient.GetChatCompletionAsync(Arg.Any<List<Message>>()).Returns(Task.FromResult(chatResponse));
            _mockConversationManager.GetMessages().Returns(new List<Message>());

            // Act
            ToolResult result = await _agentExecutor.ExecuteLoopAsync(
                "TestAgent", 
                _mockConversationManager, 
                WorkingDir, 
                null, 
                _ => { }, 
                _ => { }, 
                SystemPrompt);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Result, Is.EqualTo(expectedContent));
            _mockConversationManager.Received().AddMessage(Arg.Is<Message>(m => m.Content == expectedContent));
        }

        [Test]
        public async Task ExecuteLoopAsync_ToolCall_ExecutesToolAndContinues()
        {
            // Arrange
            var toolCallId = "call_123";
            var toolName = "read_file";
            var toolArgs = "{\"path\": \"test.txt\"}";
            var toolResult = ToolResult.Success(toolName, "File content: hello world");
            var finalAnswer = "The file contains 'hello world'.";

            // 1. First LLM response: a tool call
            var firstResponse = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice
                    {
                        Message = Message.WithToolResultAndCallList("assistant", toolCallId, ToolResult.Success(toolName), new List<ToolCall>
                            {
                                new ToolCall { Id = toolCallId, Function = new FunctionCall { Name = toolName, Arguments = toolArgs } }
                            }
                        )
                    }
                }
            };

            // 2. Second LLM response: final answer
            var secondResponse = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice { Message = new Message("assistant", finalAnswer) }
                }
            };

            _mockLlmClient.GetChatCompletionAsync(Arg.Any<List<Message>>())
                .Returns(Task.FromResult(firstResponse), Task.FromResult(secondResponse));

            _mockToolDispatcher.DispatchToolAsync(toolName, toolArgs, WorkingDir, null, _mockConversationManager)
                .Returns(Task.FromResult(toolResult));

            _mockConversationManager.GetMessages().Returns(new List<Message>());

            // Act
            var result = await _agentExecutor.ExecuteLoopAsync(
                "TestAgent", 
                _mockConversationManager, 
                WorkingDir, 
                null, 
                _ => { }, 
                _ => { }, 
                SystemPrompt);

            // Assert
            Assert.That(result.Result, Is.EqualTo(finalAnswer));
            
            // Verify tool was dispatched
            await _mockToolDispatcher.Received(1).DispatchToolAsync(toolName, toolArgs, WorkingDir, null, _mockConversationManager);
            
            // Verify tool result was added to conversation
            _mockConversationManager.Received().AddMessage(Arg.Is<Message>(m => m.Role == "tool" && m.Content == toolResult.ToString() && m.ToolCallId == toolCallId));
        }

        [Test]
        public async Task ExecuteLoopAsync_TokenLimitReached_TriggersCompaction()
        {
            // Arrange
            var toolCallId = "call_compaction";
            var toolName = "some_tool";
            var toolArgs = "{}";
            var toolResult = ToolResult.Success(toolName, "result");
            var finalAnswer = "done";

            var firstResponse = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice
                    {
                        Message = Message.WithToolCallList("assistant", toolCallId,
                            new List<ToolCall>
                            {
                                new ToolCall { Id = toolCallId, Function = new FunctionCall { Name = toolName, Arguments = toolArgs } }
                            }
                        )
                    }
                }
            };

            var secondResponse = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice { Message = Message.WithStringContent("assistant", finalAnswer) }
                }
            };

            _mockLlmClient.GetChatCompletionAsync(Arg.Any<List<Message>>())
                .Returns(Task.FromResult(firstResponse), Task.FromResult(secondResponse));

            _mockToolDispatcher.DispatchToolAsync(toolName, toolArgs, WorkingDir, null, _mockConversationManager)
                .Returns(Task.FromResult(toolResult));

            _mockConversationManager.GetMessages().Returns(new List<Message>());
            
            // Simulate that compaction is needed
            _mockConversationManager.ShouldCompact(Arg.Any<int>()).Returns(true);
            _mockConversationManager.CompactAsync(_mockLlmClient, SystemPrompt).Returns(Task.FromResult("Compacted context"));

            // Act
            await _agentExecutor.ExecuteLoopAsync(
                "TestAgent", 
                _mockConversationManager, 
                WorkingDir, 
                null, 
                _ => { }, 
                _ => { }, 
                SystemPrompt);

            // Assert
            await _mockConversationManager.Received().CompactAsync(_mockLlmClient, SystemPrompt);
        }

        [Test]
        public async Task ExecuteLoopAsync_LLMFailure_RetriesAndEventuallySucceeds()
        {
            // Arrange
            var expectedContent = "Success after retry";
            var chatResponse = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice { Message = new Message("assistant", expectedContent) }
                }
            };

            // Throw exception once, then succeed
            _mockLlmClient.GetChatCompletionAsync(Arg.Any<List<Message>>())
                .Returns(
                    _ => Task.FromException<ChatResponse>(new Exception("LLM Temporary Error")),
                    _ => Task.FromResult(chatResponse)
                );

            _mockConversationManager.GetMessages().Returns(new List<Message>());

            // Act
            var result = await _agentExecutor.ExecuteLoopAsync(
                "TestAgent", 
                _mockConversationManager, 
                WorkingDir, 
                null, 
                _ => { }, 
                _ => { }, 
                SystemPrompt);

            // Assert
            Assert.That(result.Result, Is.EqualTo(expectedContent));
            await _mockLlmClient.Received(2).GetChatCompletionAsync(Arg.Any<List<Message>>());
        }

        [Test]
        public async Task ExecuteLoopAsync_LLMFailsAllRetries_ReturnsErrorMessage()
        {
            // Arrange
            _mockLlmClient.GetChatCompletionAsync(Arg.Any<List<Message>>())
                .Returns(_ => Task.FromException<ChatResponse>(new Exception("Persistent Error")));

            _mockConversationManager.GetMessages().Returns(new List<Message>());

            // Act
            var result = await _agentExecutor.ExecuteLoopAsync(
                "TestAgent", 
                _mockConversationManager, 
                WorkingDir, 
                null, 
                _ => { }, 
                _ => { }, 
                SystemPrompt);

            // Assert
            Assert.That(result.Result, Does.Contain("AI Response was null or contained no Choices"));
        }

        [Test]
        public async Task ExecuteLoopAsync_EmptyResponse_ReturnsErrorMessage()
        {
            // Arrange
            var emptyResponse = new ChatResponse { Choices = new List<Choice>() };
            _mockLlmClient.GetChatCompletionAsync(Arg.Any<List<Message>>()).Returns(Task.FromResult(emptyResponse));
            _mockConversationManager.GetMessages().Returns(new List<Message>());

            // Act
            var result = await _agentExecutor.ExecuteLoopAsync(
                "TestAgent", 
                _mockConversationManager, 
                WorkingDir, 
                null, 
                _ => { }, 
                _ => { }, 
                SystemPrompt);

            // Assert
            Assert.That(result.Result, Does.Contain("AI Response was null or contained no Choices"));
        }
    }
}