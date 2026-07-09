using CodeMonkey.Core.Services;
using CodeMonkey.Core.Models;
using System.IO;
using System;
using System.Collections.Generic;

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
            
            bool containsWin = result.Contains("subdir\\nested.txt");
            bool containsUnix = result.Contains("subdir/nested.txt");
            Assert.That(containsWin || containsUnix, Is.True, "Should contain the file path in either Windows or Unix format");
        }

        [Test]
        public void GetFileList_FiltersBinObjAndDotFolders()
        {
            Directory.CreateDirectory(Path.Combine(_tempDir, "src"));
            File.WriteAllText(Path.Combine(_tempDir, "src", "app.cs"), "code");

            Directory.CreateDirectory(Path.Combine(_tempDir, "bin"));
            File.WriteAllText(Path.Combine(_tempDir, "bin", "app.dll"), "binary");

            Directory.CreateDirectory(Path.Combine(_tempDir, "obj"));
            File.WriteAllText(Path.Combine(_tempDir, "obj", "app.cache"), "cache");

            Directory.CreateDirectory(Path.Combine(_tempDir, "config"));
            File.WriteAllText(Path.Combine(_tempDir, "config", ".git"), "gitconfig");

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

        [Test]
        public void ReadFileWithSearch_NoMatch_ReturnsNoOccurrencesMessage()
        {
            string path = "search_no_match.txt";
            File.WriteAllText(Path.Combine(_tempDir, path), "line1\nline2\nline3");

            var result = _fileSystem.ReadFileWithSearch(path, "missing", 1, _tempDir);

            Assert.That(result, Is.EqualTo("No occurrences of 'missing' found."));
        }

        [Test]
        public void ReadFileWithSearch_MatchAtLine1_ReturnsCorrectRange()
        {
            string path = "search_line1.txt";
            File.WriteAllText(Path.Combine(_tempDir, path), "match1\nline2\nline3");

            var result = _fileSystem.ReadFileWithSearch(path, "match1", 1, _tempDir);

            string expected = $"1: match1{Environment.NewLine}2: line2";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ReadFileWithSearch_MatchAtLastLine_ReturnsCorrectRange()
        {
            string path = "search_last_line.txt";
            File.WriteAllText(Path.Combine(_tempDir, path), "line1\nline2\nmatch3");

            var result = _fileSystem.ReadFileWithSearch(path, "match3", 1, _tempDir);

            string expected = $"2: line2{Environment.NewLine}3: match3";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ReadFileWithSearch_OverlappingRanges_ReturnsMergedBlock()
        {
            string path = "search_overlapping.txt";
            File.WriteAllText(Path.Combine(_tempDir, path), "line1\nmatch2\nline3\nmatch4\nline5");

            var result = _fileSystem.ReadFileWithSearch(path, "match", 1, _tempDir);

            string expected = $"1: line1{Environment.NewLine}2: match2{Environment.NewLine}3: line3{Environment.NewLine}4: match4{Environment.NewLine}5: line5";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ReadFileWithSearch_DistantRanges_ReturnsSeparateBlocks()
        {
            string path = "search_distant.txt";
            File.WriteAllText(Path.Combine(_tempDir, path), "match1\nline2\nline3\nline4\nline5\nmatch6\nline7");

            var result = _fileSystem.ReadFileWithSearch(path, "match", 0, _tempDir);

            string expected = $"1: match1{Environment.NewLine}...{Environment.NewLine}6: match6";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ReadFileWithSearch_EmptyFile_ReturnsEmptyFileMessage()
        {
            string path = "search_empty.txt";
            File.WriteAllText(Path.Combine(_tempDir, path), "");

            var result = _fileSystem.ReadFileWithSearch(path, "match", 1, _tempDir);

            Assert.That(result, Is.EqualTo("File is empty."));
        }

        [Test]
        public void ReadFileChunked_EmptyFile_ReturnsEmptyFileMessage()
        {
            string path = "chunked_empty.txt";
            File.WriteAllText(Path.Combine(_tempDir, path), "");

            var result = _fileSystem.ReadFileChunked(path, 1, 1, _tempDir);

            Assert.That(result, Is.EqualTo("File is empty."));
        }

        [Test]
        public void ReadFileChunked_StartLineOutOfBounds_ReturnsInvalidRangeMessage()
        {
            string path = "chunked_bounds.txt";
            File.WriteAllText(Path.Combine(_tempDir, path), "L1\nL2\nL3");

            var result = _fileSystem.ReadFileChunked(path, 5, 6, _tempDir);

            Assert.That(result, Is.EqualTo("Invalid line range."));
        }

        #region WriteFileRange Tests

        private void WriteTestFile(string path, string content)
        {
            File.WriteAllText(Path.Combine(_tempDir, path), content);
        }

        private string ReadTestFile(string path)
        {
            return File.ReadAllText(Path.Combine(_tempDir, path)).TrimEnd('\r', '\n');
        }

        [Test]
        public void WriteFileRange_Replace_SingleLineFirst_ReplacesFirstLine()
        {
            string path = "replace_first.txt";
            WriteTestFile(path, "L1\nL2\nL3");
            _fileSystem.WriteFileRange(path, 1, 1, "New1", FileWriteMode.Replace, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("New1\nL2\nL3".Replace("\n", Environment.NewLine)));
        }

        [Test]
        public void WriteFileRange_Replace_SingleLineLast_ReplacesLastLine()
        {
            string path = "replace_last.txt";
            WriteTestFile(path, "L1\nL2\nL3");
            _fileSystem.WriteFileRange(path, 3, 3, "New3", FileWriteMode.Replace, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("L1\nL2\nNew3".Replace("\n", Environment.NewLine)));
        }

        [Test]
        public void WriteFileRange_Replace_EntireFile_ReplacesEverything()
        {
            string path = "replace_all.txt";
            WriteTestFile(path, "L1\nL2\nL3");
            _fileSystem.WriteFileRange(path, 1, 3, "NewContent", FileWriteMode.Replace, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("NewContent"));
        }

        [Test]
        public void WriteFileRange_Replace_RangeMiddle_ReplacesMiddleBlock()
        {
            string path = "replace_mid.txt";
            WriteTestFile(path, "L1\nL2\nL3\nL4");
            _fileSystem.WriteFileRange(path, 2, 3, "New23", FileWriteMode.Replace, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("L1\nNew23\nL4".Replace("\n", Environment.NewLine)));
        }

        [Test]
        public void WriteFileRange_InsertBefore_Line1_InsertsAtTop()
        {
            string path = "insert_before_1.txt";
            WriteTestFile(path, "L1\nL2");
            _fileSystem.WriteFileRange(path, 1, 1, "New0", FileWriteMode.InsertBefore, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("New0\nL1\nL2".Replace("\n", Environment.NewLine)));
        }

        [Test]
        public void WriteFileRange_InsertBefore_LastLine_InsertsBeforeLast()
        {
            string path = "insert_before_last.txt";
            WriteTestFile(path, "L1\nL2\nL3");
            _fileSystem.WriteFileRange(path, 3, 3, "New2.5", FileWriteMode.InsertBefore, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("L1\nL2\nNew2.5\nL3".Replace("\n", Environment.NewLine)));
        }

        [Test]
        public void WriteFileRange_InsertAfter_Line1_InsertsAtLine2()
        {
            string path = "insert_after_1.txt";
            WriteTestFile(path, "L1\nL2");
            _fileSystem.WriteFileRange(path, 1, 1, "New1.5", FileWriteMode.InsertAfter, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("L1\nNew1.5\nL2".Replace("\n", Environment.NewLine)));
        }

        [Test]
        public void WriteFileRange_InsertAfter_LastLine_AppendsToEnd()
        {
            string path = "insert_after_last.txt";
            WriteTestFile(path, "L1\nL2");
            _fileSystem.WriteFileRange(path, 2, 2, "New3", FileWriteMode.InsertAfter, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("L1\nL2\nNew3".Replace("\n", Environment.NewLine)));
        }

        [Test]
        public void WriteFileRange_Delete_SingleLineFirst_RemovesFirstLine()
        {
            string path = "delete_first.txt";
            WriteTestFile(path, "L1\nL2\nL3");
            _fileSystem.WriteFileRange(path, 1, 1, "", FileWriteMode.Delete, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("L2\nL3".Replace("\n", Environment.NewLine)));
        }

        [Test]
        public void WriteFileRange_Delete_EntireFile_EmptiesFile()
        {
            string path = "delete_all.txt";
            WriteTestFile(path, "L1\nL2\nL3");
            _fileSystem.WriteFileRange(path, 1, 3, "", FileWriteMode.Delete, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo(""));
        }

        [Test]
        public void WriteFileRange_Boundary_StartLessThanOne_ClampedToOne()
        {
            string path = "boundary_start.txt";
            WriteTestFile(path, "L1\nL2");
            _fileSystem.WriteFileRange(path, 0, 1, "New", FileWriteMode.Replace, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("New\nL2".Replace("\n", Environment.NewLine)));
        }

        [Test]
        public void WriteFileRange_Boundary_EndGreaterThanTotal_ClampedToLast()
        {
            string path = "boundary_end.txt";
            WriteTestFile(path, "L1\nL2");
            _fileSystem.WriteFileRange(path, 2, 5, "New", FileWriteMode.Replace, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("L1\nNew".Replace("\n", Environment.NewLine)));
        }

        [Test]
        public void WriteFileRange_Extreme_StartLineVeryLow_ClampedToOne()
        {
            string path = "extreme_start.txt";
            WriteTestFile(path, "L1\nL2");
            _fileSystem.WriteFileRange(path, -10, 1, "New", FileWriteMode.Replace, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("New\nL2".Replace("\n", Environment.NewLine)));
        }

        [Test]
        public void WriteFileRange_Extreme_EndLineVeryHigh_ClampedToLast()
        {
            string path = "extreme_end.txt";
            WriteTestFile(path, "L1\nL2");
            _fileSystem.WriteFileRange(path, 2, 9999, "New", FileWriteMode.Replace, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("L1\nNew".Replace("\n", Environment.NewLine)));
        }

        [Test]
        public void WriteFileRange_Extreme_BothVeryExtreme_ClampedToFullRange()
        {
            string path = "extreme_both.txt";
            WriteTestFile(path, "L1\nL2");
            _fileSystem.WriteFileRange(path, -10, 9999, "New", FileWriteMode.Replace, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("New"));
        }

        [Test]
        public void WriteFileRange_Boundary_StartGreaterThanEnd_ThrowsArgumentException()
        {
            string path = "boundary_invalid.txt";
            WriteTestFile(path, "L1\nL2");
            Assert.Throws<ArgumentException>(() => {
                _fileSystem.WriteFileRange(path, 3, 2, "New", FileWriteMode.Replace, _tempDir);
            });
        }

        [Test]
        public void WriteFileRange_Replace_MultilineContent_PreservesNewlines()
        {
            string path = "multiline_replace.txt";
            WriteTestFile(path, "L1\nL2\nL3");
            string multilineContent = "NewL1\nNewL2";
            _fileSystem.WriteFileRange(path, 2, 2, multilineContent, FileWriteMode.Replace, _tempDir);
            
            string expected = "L1\nNewL1\nNewL2\nL3".Replace("\n", Environment.NewLine);
            Assert.That(ReadTestFile(path), Is.EqualTo(expected));
        }

        [Test]
        public void WriteFileRange_Replace_EmptyFile_InsertsContent()
        {
            string path = "replace_empty.txt";
            WriteTestFile(path, "");
            _fileSystem.WriteFileRange(path, 1, 1, "NewContent", FileWriteMode.Replace, _tempDir);
            Assert.That(ReadTestFile(path), Is.EqualTo("NewContent"));
        }

        #endregion
    }
}
