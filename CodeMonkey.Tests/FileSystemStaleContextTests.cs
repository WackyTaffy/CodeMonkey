using CodeMonkey.Core.Services;
using CodeMonkey.Core.Models;
using System.IO;
using System;
using NUnit.Framework;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class FileSystemStaleContextTests
    {
        private FileSystem _fileSystem;
        private string _tempDir;

        [SetUp]
        public void Setup()
        {
            _fileSystem = new FileSystem();
            _tempDir = Path.Combine(Path.GetTempPath(), "CodeMonkeyStaleTests_" + Guid.NewGuid().ToString());
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
        public void WriteFileRange_SequentialEditsWithStaleLineNumbers_CausesCorruption()
        {
            string path = "stale_test.txt";
            // Initial state:
            // 1: Apple
            // 2: Banana
            // 3: Cherry
            string initialContent = "Apple\nBanana\nCherry";
            File.WriteAllText(Path.Combine(_tempDir, path), initialContent.Replace("\n", Environment.NewLine));

            // Simulate AI planning two edits based on the initial snapshot:
            // Plan 1: Replace Line 1 ("Apple") with "Apricot\nAvocado"
            // Plan 2: Replace Line 2 ("Banana") with "Blueberry"

            // EXECUTION 1:
            _fileSystem.WriteFileRange(path, 1, 1, "Apricot\nAvocado", FileWriteMode.Replace, _tempDir);
            // Current State:
            // 1: Apricot
            // 2: Avocado
            // 3: Banana
            // 4: Cherry

            // EXECUTION 2: Using the STALE line number (Line 2)
            _fileSystem.WriteFileRange(path, 2, 2, "Blueberry", FileWriteMode.Replace, _tempDir);
            // Current State:
            // 1: Apricot
            // 2: Blueberry
            // 3: Banana
            // 4: Cherry

            string finalContent = File.ReadAllText(Path.Combine(_tempDir, path)).TrimEnd();
            string expectedByAI = $"Apricot{Environment.NewLine}Avocado{Environment.NewLine}Blueberry{Environment.NewLine}Cherry";
            
            // The test should fail if the result matches the stale execution, 
            // proving that sequential writes with stale numbers corrupt the intent.
            Assert.That(finalContent, Is.Not.EqualTo(expectedByAI), "The file should be corrupted because the second edit targeted the wrong line.");
            
            // Verify the actual corrupted state
            string actualCorruptedState = $"Apricot{Environment.NewLine}Blueberry{Environment.NewLine}Banana{Environment.NewLine}Cherry";
            Assert.That(finalContent, Is.EqualTo(actualCorruptedState));
        }
    }
}
