using Moq;
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
        private Mock<ILLMClient> _mockLlmClient;
        private Mock<IToolManager> _mockToolManager;
        private Mock<IFileSystem> _mockFileSystem;
        private Orchestrator _orchestrator;
        private const string WorkingDir = @"C:\temp";

        [SetUp]
        public void Setup()
        {
            _mockLlmClient = new Mock<ILLMClient>();
            _mockToolManager = new Mock<IToolManager>();
            _mockFileSystem = new Mock<IFileSystem>();
            _orchestrator = new Orchestrator(_mockLlmClient.Object, _mockToolManager.Object, _mockFileSystem.Object);
        }

        [Test]
        public void BootstrapContext_SetsSystemPromptAndAddsIndex()
        {
            // Arrange
            var history = new List<Message>();
            _mockFileSystem.Setup(fs => fs.ReadFile("INDEX.md", WorkingDir)).Returns("Index content");

            // Act
            _orchestrator.BootstrapContext(history, WorkingDir);

            // Assert
            Assert.That(history.Count, Is.EqualTo(2));
            Assert.That(history[0].Role, Is.EqualTo("system"));
            Assert.That(history[0].Content, Does.Contain("You are an expert .NET developer"));
            Assert.That(history[1].Role, Is.EqualTo("context"));
            Assert.That(history[1].Content, Is.EqualTo("Index content"));
        }

        [Test]
        public void BootstrapContext_IndexNotFound_DoesNotAddIndex()
        {
            // Arrange
            var history = new List<Message>();
            _mockFileSystem.Setup(fs => fs.ReadFile("INDEX.md", WorkingDir)).Returns("File not found");

            // Act
            _orchestrator.BootstrapContext(history, WorkingDir);

            // Assert
            Assert.That(history.Count, Is.EqualTo(1));
            Assert.That(history[0].Role, Is.EqualTo("system"));
        }

        [Test]
        public async Task CompactContextAsync_SummarizesAndRebootstraps()
        {
            // Arrange
            var history = new List<Message> { new Message("user", "hi"), new Message("assistant", "hello") };
            var mockResponse = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice { Message = new Message("assistant", "This is a summary") }
                }
            };
            _mockLlmClient.Setup(c => c.GetChatCompletionAsync(It.IsAny<List<Message>>())).ReturnsAsync(mockResponse);
            _mockFileSystem.Setup(fs => fs.ReadFile("INDEX.md", WorkingDir)).Returns("Index content");

            // Act
            var result = await _orchestrator.CompactContextAsync(history, WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo("This is a summary"));
            // Verify history was cleared and contains system prompt + index + summary
            Assert.That(history.Count, Is.EqualTo(3));
            Assert.That(history[0].Role, Is.EqualTo("system"));
            Assert.That(history[1].Role, Is.EqualTo("context"));
            Assert.That(history[2].Role, Is.EqualTo("system"));
            Assert.That(history[2].Content, Does.Contain("Previous session summary: This is a summary"));
        }

        [Test]
        public async Task ProcessUserRequestAsync_SimpleResponse_ReturnsContent()
        {
            // Arrange
            string userInput = "Hello";
            var history = new List<Message>();
            var mockResponse = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice { Message = new Message("assistant", "Hi there!") }
                }
            };
            _mockLlmClient.Setup(c => c.GetChatCompletionAsync(It.IsAny<List<Message>>())).ReturnsAsync(mockResponse);

            // Act
            var result = await _orchestrator.ProcessUserRequestAsync(userInput, WorkingDir, history);

            // Assert
            Assert.That(result, Is.EqualTo("Hi there!"));
        }

        [Test]
        public async Task ProcessUserRequestAsync_ToolCall_ExecutesToolAndContinues()
        {
            // Arrange
            string userInput = "List files";
            var history = new List<Message>();
            
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

            _mockLlmClient.SetupSequence(c => c.GetChatCompletionAsync(It.IsAny<List<Message>>()))
                          .ReturnsAsync(response1)
                          .ReturnsAsync(response2);

            _mockToolManager.Setup(tm => tm.ExecuteTool("get_file_list", "{\"recursive\": \"false\"}", WorkingDir, null))
                           .Returns("a.txt\nb.txt");

            // Act
            var result = await _orchestrator.ProcessUserRequestAsync(userInput, WorkingDir, history);

            // Assert
            Assert.That(result, Is.EqualTo("Here are the files: a.txt, b.txt"));
            _mockToolManager.Verify(tm => tm.ExecuteTool("get_file_list", "{\"recursive\": \"false\"}", WorkingDir, null), Times.Once);
        }

        [Test]
        public async Task ProcessUserRequestAsync_SubagentDispatch_ExecutesSubagentAndContinues()
        {
            // Arrange
            string userInput = "Run a complex task";
            var history = new List<Message>();
            
            // First response: dispatch subagent
            var response1 = new ChatResponse
            {
                Choices = new List<Choice>
                {
                    new Choice 
                    { 
                        Message = new Message("assistant", null, new List<ToolCall> 
                        { 
                            new ToolCall { Id = "1", Function = new FunctionCall { Name = "dispatch_subagent", Arguments = "task: 'Find errors' " } } 
                        }) 
                    }
                }
            }
        };
        
        // Wait, I noticed a syntax error in my generated code. I will fix it in the next call.
        // I'll rewrite the whole file to be safe.
    }
}
