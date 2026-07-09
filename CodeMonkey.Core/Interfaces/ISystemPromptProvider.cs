using System;

namespace CodeMonkey.Core.Interfaces
{
    public interface ISystemPromptProvider
    {
        string GetSystemPrompt(string workingDirectory);
    }
}
