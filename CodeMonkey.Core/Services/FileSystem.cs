using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

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

        public string ReadFileChunked(string path, int startLine, int endLine, string workingDirectory)
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
                return $"Error reading chunk: {ex.Message}";
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
                
                var segments = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                
                bool shouldIgnore = segments.Any(s => _invalidDir.Contains(s) || s.StartsWith("."));

                if (!shouldIgnore)
                    fileList.Add(relativePath);
            }

            return fileList.Any() ? string.Join("\n", fileList) : "No files in working directory";
        }

        public string ReadFileWithSearch(string path, string searchTerm, int contextLines, string workingDirectory)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(workingDirectory, path);
            if (!File.Exists(fullPath)) return FileNotFoundMessage;

            var lines = File.ReadAllLines(fullPath);
            if (lines.Length == 0) return "File is empty.";

            var matches = lines.Select((line, index) => new { line, index })
                              .Where(x => x.line.Contains(searchTerm))
                              .Select(x => x.index)
                              .ToList();

            if (!matches.Any()) return $"No occurrences of '{searchTerm}' found.";

            var ranges = matches.Select(m => (start: Math.Max(0, m - contextLines), end: Math.Min(lines.Length - 1, m + contextLines)))
                               .OrderBy(r => r.start)
                               .ToList();

            var mergedRanges = new List<(int start, int end)>();
            if (ranges.Any())
            {
                var current = ranges[0];
                for (int i = 1; i < ranges.Count; i++)
                {
                    if (ranges[i].start <= current.end + 1)
                    {
                        current = (current.start, Math.Max(current.end, ranges[i].end));
                    }
                    else
                    {
                        mergedRanges.Add(current);
                        current = ranges[i];
                    }
                }
                mergedRanges.Add(current);
            }

            var result = new StringBuilder();
            foreach (var range in mergedRanges)
            {
                for (int i = range.start; i <= range.end; i++)
                {
                    result.AppendLine($"{i + 1}: {lines[i]}");
                }
                if (mergedRanges.IndexOf(range) < mergedRanges.Count - 1)
                {
                    result.AppendLine("...");
                }
            }

            return result.ToString().TrimEnd();
        }

        public void WriteFileRange(string path, int startLine, int endLine, string content, FileWriteMode mode, string workingDirectory)
        {
            if (startLine > endLine)
            {
                throw new ArgumentException("Start line cannot be greater than end line.");
            }

            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(workingDirectory, path);
            if (!File.Exists(fullPath)) throw new FileNotFoundException($"File not found: {fullPath}");

            var lines = File.ReadAllLines(fullPath).ToList();
            int totalLines = lines.Count;
            
            int start = Math.Clamp(startLine, 1, totalLines + 1);
            int end = totalLines == 0 ? 0 : Math.Clamp(endLine, 1, totalLines);

            var newLines = new List<string>(lines);
            var contentLines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();

            switch (mode)
            {
                case FileWriteMode.Replace:
                    if (totalLines == 0 || endLine == 0 || start > end)
                    {
                        // Treat as insertion if file is empty, endLine is 0, or range is invalid (start > end)
                        newLines.InsertRange(start - 1, contentLines);
                    }
                    else
                    {
                        newLines.RemoveRange(start - 1, end - start + 1);
                        newLines.InsertRange(start - 1, contentLines);
                    }
                    break;

                case FileWriteMode.InsertBefore:
                    int insertBeforeIdx = Math.Clamp(startLine, 1, totalLines + 1) - 1;
                    newLines.InsertRange(insertBeforeIdx, contentLines);
                    break;

                case FileWriteMode.InsertAfter:
                    int insertAfterIdx = Math.Clamp(endLine, 0, totalLines);
                    newLines.InsertRange(insertAfterIdx, contentLines);
                    break;

                case FileWriteMode.Delete:
                    if (start <= end && totalLines > 0)
                    {
                        newLines.RemoveRange(start - 1, end - start + 1);
                    }
                    // Otherwise treat as no-op
                    break;
            }

            File.WriteAllLines(fullPath, newLines);
        }
    }
}
