namespace CodeMonkey.Core.Models
{
    public record WriteFileArgs(string Path, string Content);
    public record ReadFileArgs(string Path);
    public record ReadFileChunkedArgs(string Path, int StartLine, int EndLine);
    public record ReadFileWithSearchArgs(string Path, string SearchTerm, int ContextLines);
    public record GrepArgs(string Pattern, string Path);
    public record ReadFileHeadArgs(string Path, int LineCount);
    public record ReadFileTailArgs(string Path, int LineCount);
    public record FileExistsArgs(string Path);
    public record WriteFileRangeArgs(string Path, int StartLine, int EndLine, string Content, FileWriteMode Mode);
    public record GetFileListArgs(bool Recursive = false, string SearchPattern = "*");
    public record RunCommandArgs(string Command);
}
