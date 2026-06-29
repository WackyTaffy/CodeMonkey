using System;
using System.Diagnostics;

namespace CodeMonkey.Core.Services
{
    public class ProcessRunner : CodeMonkey.Core.Interfaces.IProcessRunner
    {
        public string RunCommand(string fileName, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }
}
