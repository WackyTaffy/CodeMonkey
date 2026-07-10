using CodeMonkey.Core.Models;

namespace CodeMonkey.Core.Interfaces
{
    public interface IFileSystem
    {
        string ReadFile(string path, string workingDirectory);
        string ReadFileRange(string path, int start, int end, string workingDirectory, bool useLineCount = true);
        string ReadFileHead(string path, int count, string workingDirectory, bool useLineCount = true);
        string ReadFileTail(string path, int count, string workingDirectory, bool useLineCount = true);
        string Grep(string pattern, string path, string workingDirectory);
        string WriteFile(string path, string content, string workingDirectory);
        bool FileExists(string path, string workingDirectory);
        string GetFileList(bool recursive, string searchPattern, string workingDirectory);
        string ReadFileWithSearch(string path, string searchTerm, int contextLines, string workingDirectory);
        void WriteFileRange(string path, int start, int end, string content, FileWriteMode mode, string workingDirectory, bool useLineCount = true);
    }
}
