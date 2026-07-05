namespace CodeMonkey.Core.Interfaces
{
    public interface IFileSystem
    {
        string ReadFile(string path, string workingDirectory);
        string ReadFileRange(string path, int startLine, int endLine, string workingDirectory);
        string ReadFileHead(string path, int lineCount, string workingDirectory);
        string ReadFileTail(string path, int lineCount, string workingDirectory);
        string Grep(string pattern, string path, string workingDirectory);
        string WriteFile(string path, string content, string workingDirectory);
        bool FileExists(string path, string workingDirectory);
        string GetFileList(bool recursive, string searchPattern, string workingDirectory);
    }
}
