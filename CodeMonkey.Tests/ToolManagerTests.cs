using Moq;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Services;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class ToolManagerTests
    {
        private Mock<IFileSystem> _mockFileSystem;
        private Mock<IShell> _mockShell;
        private ToolManager _toolManager;
        private const string WorkingDir = @"C:\temp";

        [SetUp]
        public void Setup()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            _mockShell = new Mock<IShell>();
            _toolManager = new ToolManager(_mockFileSystem.Object, _mockShell.Object);
        }

        [Test]
        public void ExecuteTool_WriteFile_Success()
        {
            // Arrange
            string name = "write_file";
            string argsJson = "{\"path\": \"test.txt\", \"content\": \"hello world\"}";
            _mockFileSystem.Setup(fs => fs.WriteFile("test.txt", "hello world", WorkingDir))
                           .Returns("File written successfully");

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo("File written successfully"));
            _mockFileSystem.Verify(fs => fs.WriteFile("test.txt", "hello world", WorkingDir), Times.Once);
        }

        [Test]
        public void ExecuteTool_ReadFile_Success()
        {
            // Arrange
            string name = "read_file";
            string argsJson = "{\"path\": \"test.txt\"}";
            _mockFileSystem.Setup(fs => fs.ReadFile("test.txt", WorkingDir))
                           .Returns("file content");

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo("file content"));
            _mockFileSystem.Verify(fs => fs.ReadFile("test.txt", WorkingDir), Times.Once);
        }

        [Test]
        public void ExecuteTool_RunCommand_Success()
        {
            // Arrange
            string name = "run_command";
            string argsJson = "{\"command\": \"dir\"}";
            _mockShell.Setup(s => s.RunCommand("dir", WorkingDir))
                      .Returns("directory listing");

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo("directory listing"));
            _mockShell.Verify(s => s.RunCommand("dir", WorkingDir), Times.Once);
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
            Assert.That(result, Is.EqualTo("Error: Tool invalid_tool not found."));
        }

        [Test]
        public void ExecuteTool_InvalidJson()
        {
            // Arrange
            string name = "read_file";
            string argsJson = "{invalid json}";

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result, Does.StartWith("Error executing tool read_file:"));
        }

        [Test]
        public void ExecuteTool_ServiceException()
        {
            // Arrange
            string name = "read_file";
            string argsJson = "{\"path\": \"missing.txt\"}";
            _mockFileSystem.Setup(fs => fs.ReadFile(It.IsAny<string>(), It.IsAny<string>()))
                           .Throws(new Exception("Disk error"));

            // Act
            var result = _toolManager.ExecuteTool(name, argsJson, WorkingDir);

            // Assert
            Assert.That(result, Is.EqualTo("Error executing tool read_file: Disk error"));
        }
    }
}
