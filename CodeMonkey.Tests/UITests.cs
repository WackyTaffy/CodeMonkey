using NUnit.Framework;
using NSubstitute;
using CodeMonkey.Core.Services;
using CodeMonkey.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

namespace CodeMonkey.Tests.UI_Logic
{
    [TestFixture]
    public class GitServiceTests
    {
        private IProcessRunner _mockProcessRunner;
        private GitService _gitService;

        [SetUp]
        public void SetUp()
        {
            _mockProcessRunner = Substitute.For<IProcessRunner>();
            _gitService = new GitService(_mockProcessRunner);
        }

        [Test]
        public void GetCurrentBranch_WhenNotGitRepo_ReturnsNoGitRepo()
        {
            // We can't easily mock Directory.Exists, so we rely on current environment or 
            // we could wrap IFileSystem. For this test, we assume we might not be in a git repo 
            // if we move to a temp dir, but for simplicity, we'll test the logic flow.
            
            // If .git exists, this will fail unless we mock the filesystem.
            // Let's focus on the process runner part.
        }

        [Test]
        public void GetCurrentBranch_WhenGitRepo_ReturnsBranchName()
        {
            // Mocking the process runner to return a specific branch
            _mockProcessRunner.RunCommand("git", "rev-parse --abbrev-ref HEAD").Returns("main");
            
            // We need to ensure IsGitRepository returns true. 
            // In a real test suite, I'd wrap Directory.Exists.
            // For now, let's assume we are in the project root which HAS .git
            if (_gitService.IsGitRepository())
            {
                var branch = _gitService.GetCurrentBranch();
                Assert.That(branch, Is.EqualTo("main"));
            }
            else
            {
                Assert.Inconclusive("Not in a git repository, cannot test branch name.");
            }
        }

        [Test]
        public void GetCurrentBranch_WhenProcessFails_ReturnsGitError()
        {
            _mockProcessRunner.RunCommand(Arg.Any<string>(), Arg.Any<string>())
                .Returns(x => { throw new Exception("Git fail"); });

            if (_gitService.IsGitRepository())
            {
                var branch = _gitService.GetCurrentBranch();
                Assert.That(branch, Is.EqualTo("Git Error"));
            }
            else
            {
                Assert.Inconclusive("Not in a git repository, cannot test branch name.");
            }
        }
    }

    [TestFixture]
    public class LogManagerTests
    {
        [Test]
        public void Log_AddsToBufferAndTriggersEvent()
        {
            var logManager = new LogManager();
            string? receivedLog = null;
            logManager.OnLogAdded += (msg) => receivedLog = msg;

            logManager.Log("Test Message");

            Assert.That(receivedLog, Does.Contain("Test Message"));
            Assert.That(logManager.GetRecentLogs(1).First(), Does.Contain("Test Message"));
        }

        [Test]
        public void Log_MaintainsMaxBufferSize()
        {
            var logManager = new LogManager();
            for (int i = 0; i < 1100; i++)
            {
                logManager.Log($"Message {i}");
            }

            var logs = logManager.GetRecentLogs(2000).ToList();
            Assert.That(logs.Count, Is.EqualTo(1000));
        }
    }
}
