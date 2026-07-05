using CodeMonkey.Core.Interfaces;
using System.Text.RegularExpressions;

namespace CodeMonkey.Core.Services
{
    public class FileSystem : IFileSystem
    {
        private const string FileNotFoundMessage = "File not found.";
        private static readonly List<string> _invalidDir = new() { "bin", "obj" };

        public string ReadFile(string path, string workingDirectory)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(workingDirectory, path);
            return File.Exists(fullPath) ? File.ReadAllText(fullPath) : FileNotFoundMessage;
        }

        public string ReadFileRange(string path, int startLine, int endLine, string workingDirectory)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(workingDirectory, path);
            if (!File.Exists(fullPath)) return FileNotFoundMessage;

            try
            {
                var lines = File.ReadAllLines(fullPath);
                if (lines.Length == 0) return "File is empty.";

                int start = Math.Max(0, startLine - 1);
                int end = Math.Min(lines.Length - 1, endLine - 1);

                if (start > end) return "Invalid line range.";

                var chunk = lines.Skip(start).Take(end - start + 1);
                return $"--- Lines {start + 1} to {end + 1} ---\n" + string.Join(Environment.NewLine, chunk);
            }
            catch (Exception ex)
            {
                return $"Error reading range: {ex.Message}";
            }
        }

        public string ReadFileHead(string path, int lineCount, string workingDirectory)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(workingDirectory, path);
            if (!File.Exists(fullPath)) return FileNotFoundMessage;

            try
            {
                var lines = File.ReadAllLines(fullPath);
                var head = lines.Take(lineCount);
                return $"--- First {lineCount} lines ---\n" + string.Join(Environment.NewLine, head);
            }
            catch (Exception ex)
            {
                return $"Error reading head: {ex.Message}";
            }
        }

        public string ReadFileTail(string path, int lineCount, string workingDirectory)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(workingDirectory, path);
            if (!File.Exists(fullPath)) return FileNotFoundMessage;

            try
            {
                var lines = File.ReadAllLines(fullPath);
                var tail = lines.Skip(Math.Max(0, lines.Length - lineCount));
                return $"--- Last {lineCount} lines ---\n" + string.Join(Environment.NewLine, tail);
            }
            catch (Exception ex)
            {
                return $"Error reading tail: {ex.Message}";
            }
        }

        public string Grep(string pattern, string path, string workingDirectory)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(workingDirectory, path);
            if (!File.Exists(fullPath)) return FileNotFoundMessage;

            try
            {
                var lines = File.ReadAllLines(fullPath);
                var matches = lines
                    .Select((line, index) => new { line, index })
                    .Where(x => Regex.IsMatch(x.line, pattern))
                    .Select(x => $"{x.index + 1}: {x.line}")
                    .ToList();

                return matches.Any() ? string.Join(Environment.NewLine, matches) : "No matches found.";
            }
            catch (Exception ex)
            {
                return $"Error during grep: {ex.Message}";
            }
        }

        public string WriteFile(string path, string content, string workingDirectory)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(workingDirectory, path);
            File.WriteAllText(fullPath, content);
            return $"Successfully wrote to {fullPath}";
        }

        public bool FileExists(string path, string workingDirectory)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(workingDirectory, path);
            return File.Exists(fullPath);
        }

        public string GetFileList(bool recursive, string searchPattern, string workingDirectory)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] rawFileList = Directory.GetFiles(workingDirectory, searchPattern, searchOption);

            List<string> fileList = new();
            foreach (var filePath in rawFileList)
            {
                var relativePath = Path.GetRelativePath(workingDirectory, filePath);
                
                // Split path into segments to check for ignored directories or files
                var segments = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                
                bool shouldIgnore = segments.Any(s => _invalidDir.Contains(s) || s.StartsWith("."));

                if (!shouldIgnore)
                    fileList.Add(relativePath);
            }

            return fileList.Any() ? string.Join("\n", fileList) : "No files in working directory";
        }
    }
}
