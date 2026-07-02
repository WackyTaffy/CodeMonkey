using NSubstitute;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Services;
using CodeMonkey.Core.Models;
using System;
using NUnit.Framework;
using System.Collections.Generic;

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
        private ITokenHelper _mockTokenHelper;
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
            _mockTokenHelper = Substitute.For<ITokenHelper>();
            
            _mockUserPreferences.ActiveProfile.Returns(TrustProfile.Balanced);
            
            _toolManager = new ToolManager(_mockFileSystem, _mockShell, _mockManifestService, _mockUserPreferences, _mockSessionLedger, _mockTokenHelper);
        }

        [Test]
        public void ExecuteTool_WriteFile_Success()
        {
            // Arrange
            string name = "write_file";
            string argsJson = "{\"path\": \"test.txt\", \"content\": \"hello world\"}";
            _mockFileSystem.WriteFile("test.txt", "hello world", WorkingDir).Returns("File written successfully");
            
            var manifest = new Manifest { ActionName = name, Risk = RiskLevel.Medium, Description = "Write to file: test.txt" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, Arg.Any<TrustProfile>()).Returns(true);

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Output, Is.EqualTo("File written successfully"));
            _mockFileSystem.Received(1).WriteFile("test.txt", "hello world", WorkingDir);
            _mockSessionLedger.Received(1).RecordAction(Arg.Any<string>(), true, Arg.Any<string>());
        }

        [Test]
        public void ExecuteTool_WriteFile_PendingApproval()
        {
            // Arrange
            string name = "write_file";
            string argsJson = "{\"path\": \"test.txt\", \"content\": \"hello world\"}";
            
            var manifest = new Manifest { ActionName = name, Risk = RiskLevel.Medium, Description = "Write to file: test.txt" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, Arg.Any<TrustProfile>()).Returns(false);

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Output, Is.EqualTo($"Pending approval for tool '{name}': {manifest.Description}"));
            _mockFileSystem.DidNotReceive().WriteFile(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public void ExecuteTool_ReadFile_Success()
        {
            // Arrange
            string name = "read_file";
            string argsJson = "{\"path\": \"test.txt\"}";
            _mockFileSystem.ReadFile("test.txt", WorkingDir).Returns("file content");
            
            var manifest = new Manifest { ActionName = name, Risk = RiskLevel.Low, Description = "Read file: test.txt" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, Arg.Any<TrustProfile>()).Returns(true);

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Output, Is.EqualTo("file content"));
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

            var manifest = new Manifest { ActionName = name, Risk = RiskLevel.High, Description = "Run command: dir" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, Arg.Any<TrustProfile>()).Returns(true);

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Output, Is.EqualTo("directory listing"));
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
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Output, Is.EqualTo("Error: Tool invalid_tool not found."));
            _mockSessionLedger.Received(1).RecordAction(Arg.Any<string>(), false, Arg.Any<string>());
        }

        [Test]
        public void ExecuteTool_InvalidJson()
        {
            // Arrange
            string name = "read_file";
            string argsJson = "{invalid json}";
            
            var manifest = new Manifest { ActionName = name, Risk = RiskLevel.Low, Description = "Read file: unknown" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, Arg.Any<TrustProfile>()).Returns(true);

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Output, Does.StartWith("Error executing tool read_file:"));
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

            var manifest = new Manifest { ActionName = name, Risk = RiskLevel.Low, Description = "Read file: missing.txt" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, Arg.Any<TrustProfile>()).Returns(true);

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Output, Is.EqualTo("Error executing tool read_file: Disk error"));
            _mockSessionLedger.Received(1).RecordAction(Arg.Any<string>(), false, Arg.Any<string>());
        }
    }
}
