using System;
using System.Collections.Generic;
using System.IO;
using CodeMonkey.Core.Interfaces;

namespace CodeMonkey.Core.Services
{
    public interface IGitService
    {
        string GetCurrentBranch();
        bool IsGitRepository();
    }

    public class GitService : IGitService
    {
        private readonly IProcessRunner _processRunner;

        public GitService(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        public bool IsGitRepository()
        {
            return Directory.Exists(Path.Combine(Environment.CurrentDirectory, ".git"));
        }

        public string GetCurrentBranch()
        {
            if (!IsGitRepository()) return "No Git Repo";

            try
            {
                string result = _processRunner.RunCommand("git", "rev-parse --abbrev-ref HEAD");
                return string.IsNullOrWhiteSpace(result) ? "Unknown Branch" : result.Trim();
            }
            catch
            {
                return "Git Error";
            }
        }
    }
}
