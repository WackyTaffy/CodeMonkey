namespace CodeMonkey.Core.Interfaces
{
    public interface IShell
    {
        string RunCommand(string command, string workingDirectory);
    }
}
