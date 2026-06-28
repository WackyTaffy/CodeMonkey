using NSubstitute;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using CodeMonkey.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Linq;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class OrchestratorTests
    {
        private ILLMClient _mockLlmClient;
        private IToolManager _mockToolManager;
        private IFileSystem _mockFileSystem;
        private IConversationManager _mockConversationManager;
        private Orchestrator _orchestrator;
        private const string WorkingDir = @"C:\temp";

        [SetUp]
        public void Setup()
        {
            _mockLlmClient = Substitute.For<ILLMClient>();
            _mockToolManager = Substitute.For<IToolManager>();
            _mockFileSystem = Substitute.For<IFileSystem>();
            _mockConversationManager = Substitute.For<IConversationManager>();
            _orchestrator = new Orchestrator(_mockLlmClient, _mockToolManager, _mockFileSystem, _mockConversationManager);
        }

        [Test]
        public void BootstrapContext_SetsSystemPromptAndAddsIndex()
        {
            // Arrange
            _mockFileSystem.ReadFile("INDEX.md", WorkingDir).Returns("Index content");

            // Act
            _orchestrator.BootstrapContext(WorkingDir);

            // Assert
            _mockConversationManager.Received().AddMessage(Arg.Is<Message>(m => m.Role == "system" && m.Content != null && m.Content.Contains("You are an expert .NET developer")));
            _mockConversationManager.Received().AddMessage(Arg.Is<Message>(m => m.Role == "context" && m.Content == "Index content"));
        }

        [Test]
        public void BootstrapContext_IndexNotFound_DoesNotAddIndex()
        {
            // Arrange
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
            _mockConversationManager.ShouldCompact(Arg.Any<int>()).Returns(true);

            // Act
            var result = await _orchestrator.CompactContextAsync(WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo("Context has been compacted."));
            await _mockConversationManager.Received().CompactAsync(_mockLlmClient, Arg.Any<string>());
        }

        [Test]
        public async Task ProcessUserRequestAsync_SimpleResponse_ReturnsContent()
        {
            // Arrange
            string userInput = "Hello";
            var messages = new List<Message> { new Message("user", userInput) };
            _mockConversationManager.GetMessages().Returns(messages);
            
            var mockResponse = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice { Message = new Message("assistant", "Hi there!") }
                }
            };
            _mockLlmClient.GetChatCompletionAsync(Arg.Any<List<Message>>()).Returns(Task.FromResult(mockResponse));

            // Act
            var result = await _orchestrator.ProcessUserRequestAsync(userInput, WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo("Hi there!"));
            _mockConversationManager.Received().AddMessage(Arg.Is<Message>(m => m.Role == "user" && m.Content == userInput));
            _mockConversationManager.Received().AddMessage(Arg.Is<Message>(m => m.Role == "assistant" && m.Content == "Hi there!"));
        }

        [Test]
        public async Task ProcessUserRequestAsync_ToolCall_ExecutesToolAndContinues()
        {
            // Arrange
            string userInput = "List files";
            var messages = new List<Message> { new Message("user", userInput) };
            _mockConversationManager.GetMessages().Returns(messages);
            
            // First response: call tool
            var response1 = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice 
                    { 
                        Message = new Message("assistant", null, new List<ToolCall> 
                        { 
                            new ToolCall { Id = "1", Function = new FunctionCall { Name = "get_file_list", Arguments = "{\"recursive\": \"false\"}" } } 
                        }) 
                    }
                }
            };
            // Second response: final answer
            var response2 = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice { Message = new Message("assistant", "Here are the files: a.txt, b.txt") }
                }
            };

            _mockLlmClient.GetChatCompletionAsync(Arg.Any<List<Message>>())
                          .Returns(
                              Task.FromResult(response1),
                              Task.FromResult(response2)
                          );

            _mockToolManager.ExecuteTool("get_file_list", "{\"recursive\": \"false\"}", WorkingDir, null)
                           .Returns("a.txt\nb.txt");

            // Act
            var result = await _orchestrator.ProcessUserRequestAsync(userInput, WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo("Here are the files: a.txt, b.txt"));
            _mockToolManager.Received(1).ExecuteTool("get_file_list", "{\"recursive\": \"false\"}", WorkingDir, null);
        }

        [Test]
        public async Task ProcessUserRequestAsync_SubagentDispatch_ExecutesSubagentAndContinues()
        {
            // Arrange
            string userInput = "Run a complex task";
            var messages = new List<Message> { new Message("user", userInput) };
            _mockConversationManager.GetMessages().Returns(messages);
            
            // 1. Main agent decides to dispatch a subagent
            var response1 = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice 
                    { 
                        Message = new Message("assistant", null, new List<ToolCall> 
                        { 
                            new ToolCall { Id = "1", Function = new FunctionCall { Name = "dispatch_subagent", Arguments = "task: 'Find errors', permissions: 'read_file'" } } 
                        }) 
                    }
                }
            };

            // 2. Subagent provides a result (simulated via LLM client call for subagent loop)
            var response2 = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice { Message = new Message("assistant", "Subagent found 2 errors") }
                }
            };

            // 3. Main agent provides final response after getting subagent result
            var response3 = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice { Message = new Message("assistant", "The subagent found 2 errors, I will now fix them") }
                }
            };

            _mockLlmClient.GetChatCompletionAsync(Arg.Any<List<Message>>())
                          .Returns(
                          Task.FromResult(response1),
                          Task.FromResult(response2),
                          Task.FromResult(response3)
                          );

            _mockToolManager.ParseArguments<SubagentDispatchArgs>(Arg.Any<string>()).Returns(new SubagentDispatchArgs
            {
                Task = "Find errors",
                Permissions = new List<string> { "read_file" },
                InitialContext = new List<string>()
            });

            // Act
            var result = await _orchestrator.ProcessUserRequestAsync(userInput, WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo("The subagent found 2 errors, I will now fix them"));
        }
    }
}
