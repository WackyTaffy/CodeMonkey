namespace CodeMonkey.Core.Models
{
    public record WriteFileArgs(string Path, string Content);
    public record ReadFileArgs(string Path);
    public record ReadFileChunkedArgs(string Path, int StartLine, int EndLine);
    public record ReadFileWithSearchArgs(string Path, string SearchTerm, int ContextLines);
    public record WriteFileRangeArgs(string Path, int StartLine, int EndLine, string Content, FileWriteMode Mode);
    public record GetFileListArgs(bool Recursive = false, string SearchPattern = "*");
    public record RunCommandArgs(string Command);
}
