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
        private SecureFileSystem _secureFileSystem;

        [SetUp]
        public void Setup()
        {
            _innerFileSystem = new MockFileSystem();
            _preferences = new MockUserPreferences();
            _ledger = new MockSessionLedger();
            _secureFileSystem = new SecureFileSystem(_innerFileSystem, _preferences, _ledger);
        }

        [Test]
        public void WriteFileRange_PropagatesIOException_AndRecordsFailureInLedger()
        {
            // Arrange
            string path = "test.txt";
            string content = "new content";
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

            public string ReadFileRange(string path, int startLine, int endLine, string workingDirectory)
            {
                throw new NotImplementedException();
            }

            public string ReadFileHead(string path, int lineCount, string workingDirectory)
            {
                throw new NotImplementedException();
            }

            public string ReadFileTail(string path, int lineCount, string workingDirectory)
            {
                throw new NotImplementedException();
            }

            public string Grep(string pattern, string path, string workingDirectory)
            {
                throw new NotImplementedException();
            }
        }

        private class MockUserPreferences : IUserPreferences
        {
            public string ProjectRoot { get; set; } = "C:\\Sourcecode\\CodeMonkey";
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
        #endregion
    }
}
