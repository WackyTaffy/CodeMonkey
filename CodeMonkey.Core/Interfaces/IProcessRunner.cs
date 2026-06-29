namespace CodeMonkey.Core.Interfaces
{
    public interface IProcessRunner
    {
        string RunCommand(string fileName, string arguments);
    }
}
