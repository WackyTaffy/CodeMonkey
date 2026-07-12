using CodeMonkey.Core.Services;
using CodeMonkey.Core.Models;
using System.IO;
using System;
using NUnit.Framework;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class FileSystemSequenceTests
    {
        private FileSystem _fileSystem;
        private string _tempDir;

        [SetUp]
        public void Setup()
        {
            _fileSystem = new FileSystem();
            _tempDir = Path.Combine(Path.GetTempPath(), "CodeMonkeySequenceTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Test]
        public void WriteFileRange_MultipleSequentialEdits_MaintainsConsistency()
        {
            string path = "sequence_test.txt";
            string initialContent = "Line 1\nLine 2\nLine 3";
            File.WriteAllText(Path.Combine(_tempDir, path), initialContent.Replace("\n", Environment.NewLine));

            // 1. Replace Line 1 with two lines
            // Line 1 -> "New Line 1a\nNew Line 1b"
            // Expected state: New Line 1a, New Line 1b, Line 2, Line 3
            _fileSystem.WriteFileRange(path, 1, 1, "New Line 1a\nNew Line 1b", FileWriteMode.Replace, _tempDir);

            // 2. Replace Line 2 (originally "Line 2", now it's at position 3)
            // Position 3 -> "New Line 2"
            // Expected state: New Line 1a, New Line 1b, New Line 2, Line 3
            _fileSystem.WriteFileRange(path, 3, 3, "New Line 2", FileWriteMode.Replace, _tempDir);

            // 3. Delete Line 3 (originally "Line 3", now it's at position 4)
            // Position 4 -> ""
            // Expected state: New Line 1a, New Line 1b, New Line 2
            _fileSystem.WriteFileRange(path, 4, 4, "", FileWriteMode.Delete, _tempDir);

            string finalContent = File.ReadAllText(Path.Combine(_tempDir, path)).TrimEnd();
            string expectedContent = $"New Line 1a{Environment.NewLine}New Line 1b{Environment.NewLine}New Line 2";

            Assert.That(finalContent, Is.EqualTo(expectedContent));
        }

        [Test]
        public void WriteFileRange_MultipleSequentialEdits_InsertionsAndDeletions()
        {
            string path = "seq_insert_del.txt";
            string initialContent = "A\nB\nC";
            File.WriteAllText(Path.Combine(_tempDir, path), initialContent.Replace("\n", Environment.NewLine));

            // Insert before A: "0"
            // State: 0, A, B, C
            _fileSystem.WriteFileRange(path, 1, 1, "0", FileWriteMode.InsertBefore, _tempDir);

            // Insert after A (now at pos 2): "1.5"
            // State: 0, A, 1.5, B, C
            _fileSystem.WriteFileRange(path, 2, 2, "1.5", FileWriteMode.InsertAfter, _tempDir);

            // Delete B (now at pos 4): ""
            // State: 0, A, 1.5, C
            _fileSystem.WriteFileRange(path, 4, 4, "", FileWriteMode.Delete, _tempDir);

            string finalContent = File.ReadAllText(Path.Combine(_tempDir, path)).TrimEnd();
            string expectedContent = $"0{Environment.NewLine}A{Environment.NewLine}1.5{Environment.NewLine}C";

            Assert.That(finalContent, Is.EqualTo(expectedContent));
        }
    }
}
