using NUnit.Framework;
using CodeMonkey.Core.Services;
using System.IO;
using System;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class FileSystemTests
    {
        private FileSystem _fileSystem;
        private string _tempDir;

        [SetUp]
        public void Setup()
        {
            _fileSystem = new FileSystem();
            _tempDir = Path.Combine(Path.GetTempPath(), "CodeMonkeyTests_" + Guid.NewGuid().ToString());
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
        public void ReadFile_ExistingFile_ReturnsContent()
        {
            string path = "test.txt";
            string content = "hello world";
            File.WriteAllText(Path.Combine(_tempDir, path), content);

            var result = _fileSystem.ReadFile(path, _tempDir);

            Assert.That(result, Is.EqualTo(content));
        }

        [Test]
        public void ReadFile_NonExistingFile_ReturnsFileNotFoundMessage()
        {
            var result = _fileSystem.ReadFile("nonexistent.txt", _tempDir);

            Assert.That(result, Is.EqualTo("File not found."));
        }

        [Test]
        public void WriteFile_WritesContentToFile()
        {
            string path = "write_test.txt";
            string content = "new content";

            var result = _fileSystem.WriteFile(path, content, _tempDir);

            Assert.That(File.ReadAllText(Path.Combine(_tempDir, path)), Is.EqualTo(content));
            Assert.That(result, Does.Contain("Successfully wrote to"));
        }

        [Test]
        public void FileExists_ReturnsTrueForExistingFile()
        {
            string path = "exists.txt";
            File.WriteAllText(Path.Combine(_tempDir, path), "content");

            Assert.That(_fileSystem.FileExists(path, _tempDir), Is.True);
        }

        [Test]
        public void FileExists_ReturnsFalseForNonExistingFile()
        {
            Assert.That(_fileSystem.FileExists("not_here.txt", _tempDir), Is.False);
        }

        [Test]
        public void GetFileList_BasicListing_ReturnsFiles()
        {
            File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "1");
            File.WriteAllText(Path.Combine(_tempDir, "file2.txt"), "2");

            var result = _fileSystem.GetFileList(false, "*", _tempDir);

            Assert.That(result, Does.Contain("file1.txt"));
            Assert.That(result, Does.Contain("file2.txt"));
        }

        [Test]
        public void GetFileList_Recursive_ReturnsNestedFiles()
        {
            Directory.CreateDirectory(Path.Combine(_tempDir, "subdir"));
            File.WriteAllText(Path.Combine(_tempDir, "root.txt"), "root");
            File.WriteAllText(Path.Combine(_tempDir, "subdir", "nested.txt"), "nested");

            var result = _fileSystem.GetFileList(true, "*", _tempDir);

            Assert.That(result, Does.Contain("root.txt"));
            
            // Use a simpler check instead of .Or() to avoid NUnit version conflicts
            bool containsWin = result.Contains("subdir\\nested.txt");
            bool containsUnix = result.Contains("subdir/nested.txt");
            Assert.That(containsWin || containsUnix, Is.True, "Should contain the file path in either Windows or Unix format");
        }

        [Test]
        public void GetFileList_FiltersBinObjAndDotFolders()
        {
            // Create files in allowed folders
            Directory.CreateDirectory(Path.Combine(_tempDir, "src"));
            File.WriteAllText(Path.Combine(_tempDir, "src", "app.cs"), "code");

            // Create files in ignored folders
            Directory.CreateDirectory(Path.Combine(_tempDir, "bin"));
            File.WriteAllText(Path.Combine(_tempDir, "bin", "app.dll"), "binary");

            Directory.CreateDirectory(Path.Combine(_tempDir, "obj"));
            File.WriteAllText(Path.Combine(_tempDir, "obj", "app.cache"), "cache");

            Directory.CreateDirectory(Path.Combine(_tempDir, "config"));
            File.WriteAllText(Path.Combine(_tempDir, ".git"), "gitconfig");

            var result = _fileSystem.GetFileList(true, "*", _tempDir);

            bool containsSrc = result.Contains("src\\app.cs") || result.Contains("src/app.cs");
            Assert.That(containsSrc, Is.True);
            Assert.That(result, Does.Not.Contain("bin"));
            Assert.That(result, Does.Not.Contain("obj"));
            Assert.That(result, Does.Not.Contain(".git"));
        }

        [Test]
        public void GetFileList_EmptyDirectory_ReturnsNoFilesMessage()
        {
            var result = _fileSystem.GetFileList(false, "*", _tempDir);
            Assert.That(result, Is.EqualTo("No files in working directory"));
        }
    }
}
