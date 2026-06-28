using CodeMonkey.Core.Interfaces;

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

        public string GetFileList(string recursiveStr, string searchPattern, string workingDirectory)
        {
            bool recursive = false;
            bool.TryParse(recursiveStr, out recursive);

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
