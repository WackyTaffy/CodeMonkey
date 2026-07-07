using NUnit.Framework;
using CodeMonkey.Core.Services;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class SecureFileSystemTests
    {
        private MockFileSystem _innerFileSystem;
        private MockUserPreferences _preferences;
        private MockSessionLedger _ledger;
        private MockManifestService _manifestService;
        private SecureFileSystem _secureFileSystem;

        [SetUp]
        public void Setup()
        {
            _innerFileSystem = new MockFileSystem();
            _preferences = new MockUserPreferences();
            _ledger = new MockSessionLedger();
            _manifestService = new MockManifestService();
            _secureFileSystem = new SecureFileSystem(_innerFileSystem, _preferences, _ledger, _manifestService);
        }

        [Test]
        public void WriteFileRange_TriggersManifestAndRequestsApproval()
        {
            // Arrange
            string path = "test.txt";
            string content = "new content";
            _manifestService.ApprovalGranted = true;

            // Act
            _secureFileSystem.WriteFileRange(path, 1, 1, content, FileWriteMode.Replace, "C:\\Sourcecode\\CodeMonkey");

            // Assert
            Assert.That(_manifestService.ManifestCreated, Is.True, "Manifest should be created");
            Assert.That(_manifestService.LastRiskLevel, Is.EqualTo(RiskLevel.Medium), "Risk level should be Medium");
            Assert.That(_manifestService.ApprovalRequested, Is.True, "Approval should be requested");
            Assert.That(_innerFileSystem.WriteFileRangeCalled, Is.True, "Inner WriteFileRange should be called when approved");
        }

        [Test]
        public void WriteFileRange_ThrowsUnauthorizedAccessException_WhenApprovalDenied()
        {
            // Arrange
            string path = "test.txt";
            string content = "new content";
            _manifestService.ApprovalGranted = false;

            // Act & Assert
            Assert.Throws<UnauthorizedAccessException>(() => {
                _secureFileSystem.WriteFileRange(path, 1, 1, content, FileWriteMode.Replace, "C:\\Sourcecode\\CodeMonkey");
            });
            Assert.That(_innerFileSystem.WriteFileRangeCalled, Is.False, "Inner WriteFileRange should NOT be called when denied");
        }

        [Test]
        public void WriteFileRange_ManifestDescription_ContainsCorrectDetails()
        {
            // Arrange
            string path = "test.txt";
            string content = "new content";
            int start = 5;
            int end = 10;
            FileWriteMode mode = FileWriteMode.Replace;
            _manifestService.ApprovalGranted = true;

            // Act
            _secureFileSystem.WriteFileRange(path, start, end, content, mode, "C:\\Sourcecode\\CodeMonkey");

            // Assert
            Assert.That(_manifestService.LastDescription, Does.Contain("Surgical edit"), "Description should mention surgical edit");
            Assert.That(_manifestService.LastDescription, Does.Contain(mode.ToString()), "Description should mention the mode");
            Assert.That(_manifestService.LastDescription, Does.Contain($"{start}-{end}"), "Description should mention the range");
            Assert.That(_manifestService.LastDescription, Does.Contain(path), "Description should mention the path");
        }

        [Test]
        public void WriteFileRange_RecordsSuccessInLedger()
        {
            // Arrange
            string path = "test.txt";
            string content = "new content";
            _manifestService.ApprovalGranted = true;

            // Act
            _secureFileSystem.WriteFileRange(path, 1, 1, content, FileWriteMode.Replace, "C:\\Sourcecode\\CodeMonkey");

            // Assert
            Assert.That(_ledger.RecordedActions, Has.Some.Matches< (string Action, bool Success, string Message)>(
                a => a.Action.Contains("WriteFileRange") && a.Success == true), "Success should be recorded in the ledger");
        }

        [Test]
        public void WriteFileRange_PropagatesIOException_AndRecordsFailureInLedger()
        {
            // Arrange
            string path = "test.txt";
            string content = "new content";
            _manifestService.ApprovalGranted = true;
            _innerFileSystem.ShouldThrowIOException = true;

            // Act & Assert
            Assert.Throws<IOException>(() => {
                _secureFileSystem.WriteFileRange(path, 1, 1, content, FileWriteMode.Replace, "C:\\Sourcecode\\CodeMonkey");
            });

            Assert.That(_ledger.RecordedActions, Has.Some.Matches< (string Action, bool Success, string Message)>(
                a => a.Action.Contains("WriteFileRange") && a.Success == false && a.Message.Contains("Mock IO Exception")), 
                "Failure should be recorded in the ledger");
        }

        #region Mocks

        private class MockFileSystem : IFileSystem
        {
            public bool WriteFileRangeCalled { get; private set; }
            public bool ShouldThrowIOException { get; set; }
            public string ReadFile(string path, string workingDirectory) => "";
            public string ReadFileChunked(string path, int startLine, int endLine, string workingDirectory) => "";
            public string WriteFile(string path, string content, string workingDirectory) => "";
            public bool FileExists(string path, string workingDirectory) => true;
            public string GetFileList(bool recursive, string searchPattern, string workingDirectory) => "";
            public string ReadFileWithSearch(string path, string searchTerm, int contextLines, string workingDirectory) => "";
            public void WriteFileRange(string path, int startLine, int endLine, string content, FileWriteMode mode, string workingDirectory)
            {
                if (ShouldThrowIOException) throw new IOException("Mock IO Exception");
                WriteFileRangeCalled = true;
            }
        }

        private class MockUserPreferences : IUserPreferences
        {
            public string ProjectRoot { get; set; } = "C:\\Sourcecode\\CodeMonkey";
            public TrustProfile ActiveProfile { get; set; } = TrustProfile.Balanced;
            public void Save() { }
            public void Load() { }
        }

        private class MockSessionLedger : ISessionLedger
        {
            public List<(string Action, bool Success, string Message)> RecordedActions = new();
            public void RecordAction(string action, bool success, string message) 
            { 
                RecordedActions.Add((action, success, message)); 
            }
            public IEnumerable<(string Action, bool Success, string Details, DateTime Timestamp)> GetHistory() => 
                RecordedActions.Select(a => (a.Action, a.Success, a.Message, DateTime.Now));
            public void Clear() => RecordedActions.Clear();
        }

        private class MockManifestService : IManifestService
        {
            public bool ManifestCreated { get; private set; }
            public bool ApprovalRequested { get; private set; }
            public RiskLevel LastRiskLevel { get; private set; }
            public string LastDescription { get; private set; }
            public bool ApprovalGranted { get; set; }

            public Manifest CreateManifest(string action, RiskLevel risk, string description, params string[] args)
            {
                ManifestCreated = true;
                LastRiskLevel = risk;
                LastDescription = description;
                return new Manifest { ActionName = action, Risk = risk, Description = description };
            }

            public bool RequestApproval(Manifest manifest, TrustProfile profile)
            {
                ApprovalRequested = true;
                return ApprovalGranted;
            }

            public IEnumerable<Manifest> GetPendingManifests() => Enumerable.Empty<Manifest>();
            public void ApproveManifest(Guid id) { }
            public void RejectManifest(Guid id) { }
        }
        #endregion
    }
}
