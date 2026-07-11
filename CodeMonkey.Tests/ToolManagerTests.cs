using NSubstitute;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Services;
using CodeMonkey.Core.Models;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class ToolManagerTests
    {
        private IFileSystem _mockFileSystem;
        private IShell _mockShell;
        private IUserPreferences _mockUserPreferences;
        private ITokenHelper _mockTokenHelper;
        private ToolManager _toolManager;
        private const string WorkingDir = @"C:\temp";

        [SetUp]
        public void Setup()
        {
            _mockFileSystem = Substitute.For<IFileSystem>();
            _mockShell = Substitute.For<IShell>();
            _mockUserPreferences = Substitute.For<IUserPreferences>();
            _mockTokenHelper = Substitute.For<ITokenHelper>();
            
            _toolManager = new ToolManager(_mockFileSystem, _mockShell, _mockUserPreferences, _mockTokenHelper);
        }

        [Test]
        public void ExecuteTool_WriteFile_Success()
        {
            // Arrange
            string name = "write_file";
            string argsJson = "{\"path\": \"test.txt\", \"content\": \"hello world\"}";
            _mockFileSystem.WriteFile("test.txt", "hello world", WorkingDir).Returns("File written successfully");

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Result, Is.EqualTo("File written successfully"));
            _mockFileSystem.Received(1).WriteFile("test.txt", "hello world", WorkingDir);
        }

        [Test]
        public void ExecuteTool_ReadFile_Success()
        {
            // Arrange
            string name = "read_file";
            string argsJson = "{\"path\": \"test.txt\"}";
            _mockFileSystem.ReadFile("test.txt", WorkingDir).Returns("file content");
            
            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Result, Is.EqualTo("file content"));
            _mockFileSystem.Received(1).ReadFile("test.txt", WorkingDir);
        }

        [Test]
        public void ExecuteTool_RunCommand_Success()
        {
            // Arrange
            string name = "run_command";
            string argsJson = "{\"command\": \"dir\"}";
            _mockShell.RunCommand("dir", WorkingDir).Returns("directory listing");

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Result, Is.EqualTo("directory listing"));
            _mockShell.Received(1).RunCommand("dir", WorkingDir);
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
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Result, Is.EqualTo("Error: Tool invalid_tool not found."));
        }

        [Test]
        public void ExecuteTool_InvalidJson()
        {
            // Arrange
            string name = "read_file";
            string argsJson = "{invalid json}";

            // Act
            ToolResult result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Result, Contains.Substring("Invalid arguments"));
        }

        [Test]
        public void ExecuteTool_ServiceException()
        {
            // Arrange
            string name = "read_file";
            string argsJson = "{\"path\": \"missing.txt\"}";
            _mockFileSystem.ReadFile(Arg.Any<string>(), Arg.Any<string>())
                           .Returns(_ => throw new Exception("Disk error"));

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Result, Contains.Substring("Disk error"));
        }

        [Test]
        public void ExecuteTool_WriteFileRange_StringEnum_Success()
        {
            // Arrange
            string name = "write_file_range";
            // Using string for mode instead of integer
            string argsJson = "{\"path\": \"test.txt\", \"startLine\": 1, \"endLine\": 5, \"content\": \"new content\", \"mode\": \"Replace\"}";

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result.Result, Does.Not.Contain("Error:"));
            Assert.That(result.Result, Does.Contain("Successfully updated"));
            _mockFileSystem.Received(1).WriteFileRange("test.txt", 1, 5, "new content", Arg.Any<CodeMonkey.Core.Models.FileWriteMode>(), WorkingDir);
        }
    }
}
