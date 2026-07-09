namespace CodeMonkey.Core.Utility
{
    public static class PathGuard
    {
        /// <summary>
        /// Normalizes and validates that a path is within the specified root directory.
        /// Prevents directory traversal attacks.
        /// </summary>
        /// <param name="root">The trusted root directory.</param>
        /// <param name="path">The path to validate.</param>
        /// <returns>The full normalized path if valid; otherwise, throws an UnauthorizedAccessException.</returns>
        public static string ValidateAndNormalize(string root, string path)
        {
            if (string.IsNullOrWhiteSpace(root))
                throw new ArgumentException("Root directory cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.");

            // 1. Get absolute paths
            string absoluteRoot = Path.GetFullPath(root);
            string absolutePath = Path.GetFullPath(path);

            // Ensure the root ends with a directory separator to avoid "C:\Project" matching "C:\ProjectFiles"
            string rootWithSeparator = absoluteRoot;
            if (!rootWithSeparator.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                rootWithSeparator += Path.DirectorySeparatorChar;
            }

            // 2. Check if the absolute path is exactly the root or starts with the root separator
            if (absolutePath.Equals(absoluteRoot, StringComparison.OrdinalIgnoreCase) || 
                absolutePath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                return absolutePath;
            }

            throw new UnauthorizedAccessException($"Access denied: Path '{path}' is outside of the trusted root '{root}'.");
        }

        /// <summary>
        /// Checks if a path is within the root directory without throwing an exception.
        /// </summary>
        public static bool IsWithinRoot(string root, string path)
        {
            try
            {
                return ValidateAndNormalize(root, path) != null;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
