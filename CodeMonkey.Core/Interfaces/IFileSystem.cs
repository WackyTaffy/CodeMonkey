namespace CodeMonkey.Core.Interfaces
{
    public interface IFileSystem
    {
        string ReadFile(string path, string workingDirectory);
        string ReadFileChunked(string path, int startLine, int endLine, string workingDirectory);
        string WriteFile(string path, string content, string workingDirectory);
        bool FileExists(string path, string workingDirectory);
        string GetFileList(bool recursive, string searchPattern, string workingDirectory);
    }
}
