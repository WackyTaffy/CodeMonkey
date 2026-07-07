using System;

namespace CodeMonkey.Core.Interfaces
{
    public interface IPromptProvider
    {
        string GetSystemPrompt(string workingDirectory);
        string GetSubagentSystemPrompt(string name, string task, string workingDirectory);
    }
}
