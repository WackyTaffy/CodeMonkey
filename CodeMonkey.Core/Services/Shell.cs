using CodeMonkey.Core.Interfaces;
using System.Diagnostics;

namespace CodeMonkey.Core.Services
{
    public class Shell : IShell
    {
        public string RunCommand(string command, string workingDirectory)
        {
            if (!Directory.Exists(workingDirectory))
            {
                return $"Error: The working directory '{workingDirectory}' does not exist.";
            }

            var processInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(processInfo);
                if (process == null) return "Error: Failed to start process.";
                
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(error)
                       ? "Command executed with no output."
                       : $"{output}\n{error}".Trim();
            }
            catch (Exception ex)
            {
                return $"Failed to execute command: {ex.Message}";
            }
        }
    }
}
