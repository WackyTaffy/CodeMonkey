using NSubstitute;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using CodeMonkey.Core.Services;
using CodeMonkey.Core.Utility;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class OrchestratorTests
    {
        private IAgentExecutor _mockAgentExecutor;
        private IPromptProvider _mockPromptProvider;
        private IFileSystem _mockFileSystem;
        private IConversationManager _mockConversationManager;
        private IContextGuard _mockContextGuard;
        private ILLMClient _mockLlmClient;
        private IToolManager _mockToolManager;
        private Orchestrator _orchestrator;
        private const string WorkingDir = @"C:\temp";

        [SetUp]
        public void Setup()
        {
            _mockAgentExecutor = Substitute.For<IAgentExecutor>();
            _mockPromptProvider = Substitute.For<IPromptProvider>();
            _mockFileSystem = Substitute.For<IFileSystem>();
            _mockConversationManager = Substitute.For<IConversationManager>();
            _mockContextGuard = Substitute.For<IContextGuard>();
            _mockLlmClient = Substitute.For<ILLMClient>();
            _mockToolManager = Substitute.For<IToolManager>();
            
            _orchestrator = new Orchestrator(
                _mockAgentExecutor, 
                _mockPromptProvider, 
                _mockFileSystem, 
                _mockConversationManager);
        }

        [Test]
        public void BootstrapContext_SetsSystemPromptAndAddsIndex()
        {
            // Arrange
            string expectedPrompt = "You are an expert .NET developer";
            _mockPromptProvider.GetSystemPrompt(WorkingDir).Returns(expectedPrompt);
            _mockFileSystem.ReadFile("INDEX.md", WorkingDir).Returns("Index content");

            // Act
            _orchestrator.BootstrapContext(WorkingDir);

            // Assert
            _mockConversationManager.Received().AddMessage(Arg.Is<Message>(m => m.Role == "system" && m.Content == expectedPrompt));
            _mockConversationManager.Received().AddMessage(Arg.Is<Message>(m => m.Role == "context" && m.Content == "Index content"));
        }

        [Test]
        public void BootstrapContext_IndexNotFound_DoesNotAddIndex()
        {
            // Arrange
            _mockPromptProvider.GetSystemPrompt(WorkingDir).Returns("System Prompt");
            _mockFileSystem.ReadFile("INDEX.md", WorkingDir).Returns("File not found");

            // Act
            _orchestrator.BootstrapContext(WorkingDir);

            // Assert
            _mockConversationManager.Received().AddMessage(Arg.Is<Message>(m => m.Role == "system"));
            _mockConversationManager.DidNotReceive().AddMessage(Arg.Is<Message>(m => m.Role == "context"));
            
        }

        [Test]
        public async Task CompactContextAsync_CallsConversationManagerCompact()
        {
            // Arrange
            string expectedSummary = "Context has been compacted.";
            string systemPrompt = "System Prompt";
            
            _mockAgentExecutor.Client.Returns(_mockLlmClient);
            _mockPromptProvider.GetSystemPrompt(WorkingDir).Returns(systemPrompt);
            _mockConversationManager.CompactAsync(_mockLlmClient, systemPrompt).Returns(Task.FromResult(expectedSummary));

            // Act
            var result = await _orchestrator.CompactContextAsync(WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo(expectedSummary));
            await _mockConversationManager.Received().CompactAsync(_mockLlmClient, systemPrompt);
        }

        [Test]
        public async Task ProcessUserRequestAsync_DelegatesToAgentExecutor()
        {
            // Arrange
            string userInput = "Hello";
            string systemPrompt = "System Prompt";
            string expectedResponse = "Hi there!";
            
            _mockPromptProvider.GetSystemPrompt(WorkingDir).Returns(systemPrompt);
            _mockAgentExecutor.ExecuteLoopAsync(
                "Main Agent", 
                _mockConversationManager, 
                WorkingDir, 
                null, 
                Arg.Any<Action<string>>(), 
                Arg.Any<Action<ToolResult>>(), 
                systemPrompt)
                .Returns(Task.FromResult(ToolResult.Success("Main Agent", expectedResponse)));

            // Act
            var result = await _orchestrator.ProcessUserRequestAsync(userInput, WorkingDir);

            // Assert
            Assert.That(result.Result, Is.EqualTo(expectedResponse));
            _mockConversationManager.Received().AddMessage(Arg.Is<Message>(m => m.Role == "user" && m.Content == userInput));
            await _mockAgentExecutor.Received(1).ExecuteLoopAsync(
                "Main Agent", 
                _mockConversationManager, 
                WorkingDir, 
                null, 
                Arg.Any<Action<string>>(), 
                Arg.Any<Action<ToolResult>>(), 
                systemPrompt);
        }

        [Test]
        public async Task ProcessUserRequestAsync_ToolOutputTooLarge_TruncatesAndAddsToConversation()
        {
            // Arrange
            string userInput = "Get large output";
            var messages = new List<Message> { Message.AsUserMessage(userInput) };
            _mockConversationManager.GetMessages().Returns(messages);

            var response1 = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice
                    {
                        Message = Message.AsAssistantMessage(new List<ToolCall>
                            {
                                new ToolCall { Id = "1", Function = new FunctionCall { Name = "get_large_output", Arguments = "{}" } }
                            } 
                        )
                    }
                }
            };
            var response2 = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice { Message = Message.AsAssistantMessage("I got the truncated output.") }
                }
            };

            _mockLlmClient.GetChatCompletionAsync(Arg.Any<List<Message>>())
                          .Returns(
                              Task.FromResult(response1),
                              Task.FromResult(response2)
                          );

            string oversizedOutput = new string('A', 20000);
            string truncatedOutput = "Truncated version of " + oversizedOutput.Substring(0, 10) + "... [TRUNCATED]";
            
            _mockToolManager.ExecuteTool("get_large_output", "{}", WorkingDir, null)
                           .Returns(ToolResult.Success("get_large_output", oversizedOutput));
            
            _mockContextGuard.Guard(oversizedOutput, ContextConstants.MaxToolOutputTokens)
                           .Returns(truncatedOutput);

            // Act
            var result = await _orchestrator.ProcessUserRequestAsync(userInput, WorkingDir);

            // Assert
            Assert.That(result.Result, Is.EqualTo("I got the truncated output."));
            _mockConversationManager.Received().AddMessage(Arg.Is<Message>(m => m.Role == "tool" && m.Content == truncatedOutput && m.ToolCallId == "1"));
        }
    }
}
