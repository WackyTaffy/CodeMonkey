using NSubstitute;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using CodeMonkey.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using System;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class OrchestratorTests
    {
        private IAgentExecutor _mockAgentExecutor;
        private IPromptProvider _mockPromptProvider;
        private IFileSystem _mockFileSystem;
        private IConversationManager _mockConversationManager;
        private Orchestrator _orchestrator;
        private const string WorkingDir = @"C:\temp";

        [SetUp]
        public void Setup()
        {
            _mockAgentExecutor = Substitute.For<IAgentExecutor>();
            _mockPromptProvider = Substitute.For<IPromptProvider>();
            _mockFileSystem = Substitute.For<IFileSystem>();
            _mockConversationManager = Substitute.For<IConversationManager>();
            
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
            var mockLlmClient = Substitute.For<ILLMClient>();
            
            _mockAgentExecutor.Client.Returns(mockLlmClient);
            _mockPromptProvider.GetSystemPrompt(WorkingDir).Returns(systemPrompt);
            _mockConversationManager.CompactAsync(mockLlmClient, systemPrompt).Returns(Task.FromResult(expectedSummary));

            // Act
            var result = await _orchestrator.CompactContextAsync(WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo(expectedSummary));
            await _mockConversationManager.Received().CompactAsync(mockLlmClient, systemPrompt);
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
                .Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _orchestrator.ProcessUserRequestAsync(userInput, WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo(expectedResponse));
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
    }
}
