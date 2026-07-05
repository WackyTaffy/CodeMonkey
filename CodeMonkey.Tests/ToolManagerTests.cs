using NSubstitute;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Services;
using System;
using CodeMonkey.Core.Models;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class ToolManagerTests
    {
        private IFileSystem _mockFileSystem;
        private IShell _mockShell;
        private IManifestService _mockManifestService;
        private IUserPreferences _mockUserPreferences;
        private ISessionLedger _mockSessionLedger;
        private ToolManager _toolManager;
        private const string WorkingDir = @"C:\temp";

        [SetUp]
        public void Setup()
        {
            _mockFileSystem = Substitute.For<IFileSystem>();
            _mockShell = Substitute.For<IShell>();
            _mockManifestService = Substitute.For<IManifestService>();
            _mockUserPreferences = Substitute.For<IUserPreferences>();
            _mockSessionLedger = Substitute.For<ISessionLedger>();
            _toolManager = new ToolManager(_mockFileSystem, _mockShell, _mockManifestService, _mockUserPreferences, _mockSessionLedger);
        }

        [Test]
        public void ExecuteTool_WriteFile_Success()
        {
            // Arrange
            string name = "write_file";
            string argsJson = "{\"path\": \"test.txt\", \"content\": \"hello world\"}";
            _mockFileSystem.WriteFile("test.txt", "hello world", WorkingDir).Returns("File written successfully");
            
            // Setup manifest to auto-approve
            var manifest = new CodeMonkey.Core.Models.Manifest { ActionName = name, Risk = CodeMonkey.Core.Models.RiskLevel.Medium, Description = "test description" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<CodeMonkey.Core.Models.RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, Arg.Any<CodeMonkey.Core.Models.TrustProfile>()).Returns(true);

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.EqualTo("File written successfully"));
            _mockFileSystem.Received(1).WriteFile("test.txt", "hello world", WorkingDir);
            _mockSessionLedger.Received(1).RecordAction(Arg.Any<string>(), true, Arg.Any<string>());
        }

        [Test]
        public void ExecuteTool_ReadFile_Success()
        {
            // Arrange
            string name = "read_file";
            string argsJson = "{\"path\": \"test.txt\"}";
            _mockFileSystem.ReadFile("test.txt", WorkingDir).Returns("file content");
            
            var manifest = new CodeMonkey.Core.Models.Manifest { ActionName = name, Risk = CodeMonkey.Core.Models.RiskLevel.Low, Description = "test description" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<CodeMonkey.Core.Models.RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, Arg.Any<CodeMonkey.Core.Models.TrustProfile>()).Returns(true);

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.EqualTo("file content"));
            _mockFileSystem.Received(1).ReadFile("test.txt", WorkingDir);
            _mockSessionLedger.Received(1).RecordAction(Arg.Any<string>(), true, Arg.Any<string>());
        }

        [Test]
        public void ExecuteTool_RunCommand_Success()
        {
            // Arrange
            string name = "run_command";
            string argsJson = "{\"command\": \"dir\"}";
            _mockShell.RunCommand("dir", WorkingDir).Returns("directory listing");

            var manifest = new CodeMonkey.Core.Models.Manifest { ActionName = "Shell: run_command", Risk = CodeMonkey.Core.Models.RiskLevel.High, Description = "test description" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<CodeMonkey.Core.Models.RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, Arg.Any<CodeMonkey.Core.Models.TrustProfile>()).Returns(true);

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.EqualTo("directory listing"));
            _mockShell.Received(1).RunCommand("dir", WorkingDir);
            _mockSessionLedger.Received(1).RecordAction(Arg.Any<string>(), true, Arg.Any<string>());
        }

        [Test]
        public void ExecuteTool_UnknownTool()
        {
            // Arrange
            string name = "invalid_tool";
            string argsJson = "{}";

            // Act
            ToolResult result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Result, Is.EqualTo("Error: Tool invalid_tool not found."));
            _mockSessionLedger.Received(1).RecordAction(Arg.Any<string>(), false, Arg.Any<string>());
        }

        [Test]
        public void ExecuteTool_InvalidJson()
        {
            // Arrange
            string name = "read_file";
            string argsJson = "{invalid json}";
            
            var manifest = new CodeMonkey.Core.Models.Manifest { ActionName = name, Risk = CodeMonkey.Core.Models.RiskLevel.Low, Description = "test description" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<CodeMonkey.Core.Models.RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, Arg.Any<CodeMonkey.Core.Models.TrustProfile>()).Returns(true);

            // Act
            ToolResult result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Result, Does.StartWith("Error executing tool read_file:"));
            _mockSessionLedger.Received(1).RecordAction(Arg.Any<string>(), false, Arg.Any<string>());
        }

        [Test]
        public void ExecuteTool_ServiceException()
        {
            // Arrange
            string name = "read_file";
            string argsJson = "{\"path\": \"missing.txt\"}";
            _mockFileSystem.ReadFile(Arg.Any<string>(), Arg.Any<string>())
                           .Returns(_ => throw new Exception("Disk error"));

            var manifest = new CodeMonkey.Core.Models.Manifest { ActionName = name, Risk = CodeMonkey.Core.Models.RiskLevel.Low, Description = "test description" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<CodeMonkey.Core.Models.RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, Arg.Any<CodeMonkey.Core.Models.TrustProfile>()).Returns(true);

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Result, Is.EqualTo("Error executing tool read_file: Disk error"));
            _mockSessionLedger.Received(1).RecordAction(Arg.Any<string>(), false, Arg.Any<string>());
        }
    }
}
