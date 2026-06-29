using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace CodeMonkey.Core.Services
{
    public interface IGitService
    {
        string GetCurrentBranch();
        bool IsGitRepository();
    }

    public class GitService : IGitService
    {
        public bool IsGitRepository()
        {
            // In a real scenario, we might want to check multiple directories up the tree
            return Directory.Exists(Path.Combine(Environment.CurrentDirectory, ".git"));
        }

        public string GetCurrentBranch()
        {
            if (!IsGitRepository()) return "No Git Repo";

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "rev-parse --abbrev-ref HEAD",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string result = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return string.IsNullOrEmpty(result) ? "Unknown Branch" : result;
            }
            catch
            {
                return "Git Error";
            }
        }
    }
}
