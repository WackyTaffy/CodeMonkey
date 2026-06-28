namespace CodeMonkey.Core.Models
{
    public record WriteFileArgs(string Path, string Content);
    public record ReadFileArgs(string Path);
    public record GetFileListArgs(bool Recursive = false, string SearchPattern = "*");
    public record RunCommandArgs(string Command);
}
