using NUnit.Framework;
using NSubstitute;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Services;
using CodeMonkey.Core.Models;
using System;
using System.Collections.Generic;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class ConfidenceGatingTests
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
            _toolManager = new ToolManager(_mockFileSystem, _mockShell, _mockManifestService, _mockUserPreferences, _mockSessionLedger, _mockTokenHelper);
        }

        [Test]
        public void ExecuteTool_StrictProfile_LowRisk_AutoApproves()
        {
            // Arrange
            _mockUserPreferences.ActiveProfile.Returns(TrustProfile.Strict);
            string tool = "read_file";
            string args = "{\"path\": \"test.txt\"}";
            
            var manifest = new Manifest { ActionName = tool, Risk = RiskLevel.Low, Description = "test description" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, TrustProfile.Strict).Returns(true);
            
            _mockFileSystem.ReadFile("test.txt", WorkingDir).Returns("content");

            // Act
            var result = _toolManager.ExecuteTool(tool, args, WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo("content"));
            _mockManifestService.Received(1).RequestApproval(Arg.Any<Manifest>(), TrustProfile.Strict);
        }

        [Test]
        public void ExecuteTool_StrictProfile_MediumRisk_RequiresApproval()
        {
            // Arrange
            _mockUserPreferences.ActiveProfile.Returns(TrustProfile.Strict);
            string tool = "write_file";
            string args = "{\"path\": \"test.txt\", \"content\": \"hello\"}";
            
            var manifest = new Manifest { ActionName = tool, Risk = RiskLevel.Medium, Description = "test description" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, TrustProfile.Strict).Returns(false);

            // Act
            var result = _toolManager.ExecuteTool(tool, args, WorkingDir);

            // Assert
            Assert.That(result, Does.Contain("requires manual approval"));
            _mockFileSystem.DidNotReceive().WriteFile(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public void ExecuteTool_BalancedProfile_MediumRisk_AutoApproves()
        {
            // Arrange
            _mockUserPreferences.ActiveProfile.Returns(TrustProfile.Balanced);
            string tool = "write_file";
            string args = "{\"path\": \"test.txt\", \"content\": \"hello\"}";
            
            var manifest = new Manifest { ActionName = tool, Risk = RiskLevel.Medium, Description = "test description" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, TrustProfile.Balanced).Returns(true);
            
            _mockFileSystem.WriteFile("test.txt", "hello", WorkingDir).Returns("Success");

            // Act
            var result = _toolManager.ExecuteTool(tool, args, WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo("Success"));
        }

        [Test]
        public void ExecuteTool_BalancedProfile_HighRisk_RequiresApproval()
        {
            // Arrange
            _mockUserPreferences.ActiveProfile.Returns(TrustProfile.Balanced);
            string tool = "run_command";
            string args = "{\"command\": \"dir\"}";
            
            var manifest = new Manifest { ActionName = "Shell: run_command", Risk = RiskLevel.High, Description = "test description" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, TrustProfile.Balanced).Returns(false);

            // Act
            var result = _toolManager.ExecuteTool(tool, args, WorkingDir);

            // Assert
            Assert.That(result, Does.Contain("requires manual approval"));
            _mockShell.DidNotReceive().RunCommand(Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public void ExecuteTool_TrustingProfile_HighRisk_AutoApprovesUnlessDestructive()
        {
            // Arrange
            _mockUserPreferences.ActiveProfile.Returns(TrustProfile.Trusting);
            string tool = "run_command";
            string args = "{\"command\": \"ls\"}";
            
            var manifest = new Manifest { ActionName = "Shell: run_command", Risk = RiskLevel.High, Description = "test description" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, TrustProfile.Trusting).Returns(true);
            
            _mockShell.RunCommand("ls", WorkingDir).Returns("listing");

            // Act
            var result = _toolManager.ExecuteTool(tool, args, WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo("listing"));
        }

        [Test]
        public void ExecuteTool_TrustingProfile_DestructiveCommand_RequiresApproval()
        {
            // Arrange
            _mockUserPreferences.ActiveProfile.Returns(TrustProfile.Trusting);
            string tool = "run_command";
            string args = "{\"command\": \"rm -rf /\"}";
            
            var manifest = new Manifest { ActionName = "Shell: run_command", Risk = RiskLevel.High, Description = "test description" };
            _mockManifestService.CreateManifest(Arg.Any<string>(), Arg.Any<RiskLevel>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(manifest);
            _mockManifestService.RequestApproval(manifest, TrustProfile.Trusting).Returns(false);

            // Act
            var result = _toolManager.ExecuteTool(tool, args, WorkingDir);

            // Assert
            Assert.That(result, Does.Contain("requires manual approval"));
            _mockShell.DidNotReceive().RunCommand(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
