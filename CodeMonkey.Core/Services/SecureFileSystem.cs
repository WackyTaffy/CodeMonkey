using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Utility;

namespace CodeMonkey.Core.Services
{
    public class SecureFileSystem : IFileSystem
    {
        private readonly IFileSystem _inner;
        private readonly IUserPreferences _preferences;
        private readonly ISessionLedger _ledger;

        public SecureFileSystem(IFileSystem inner, IUserPreferences preferences, ISessionLedger ledger)
        {
            _inner = inner;
            _preferences = preferences;
            _ledger = ledger;
        }

        private string ValidatePath(string path, string workingDirectory)
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(workingDirectory, path);
            return PathGuard.ValidateAndNormalize(_preferences.ProjectRoot, fullPath);
        }

        public string ReadFile(string path, string workingDirectory)
        {
            try
            {
                string validatedPath = ValidatePath(path, workingDirectory);
                return _inner.ReadFile(validatedPath, ""); // workingDirectory already handled by ValidatePath
            }
            catch (Exception ex)
            {
                _ledger.RecordAction($"ReadFile: {path}", false, ex.Message);
                throw;
            }
        }

        public string ReadFileRange(string path, int startLine, int endLine, string workingDirectory)
        {
            try
            {
                string validatedPath = ValidatePath(path, workingDirectory);
                return _inner.ReadFileRange(validatedPath, startLine, endLine, "");
            }
            catch (Exception ex)
            {
                _ledger.RecordAction($"ReadFileRange: {path} (lines {startLine}-{endLine})", false, ex.Message);
                throw;
            }
        }

        public string ReadFileHead(string path, int lineCount, string workingDirectory)
        {
            try
            {
                string validatedPath = ValidatePath(path, workingDirectory);
                return _inner.ReadFileHead(validatedPath, lineCount, "");
            }
            catch (Exception ex)
            {
                _ledger.RecordAction($"ReadFileHead: {path} (lines {lineCount})", false, ex.Message);
                throw;
            }
        }

        public string ReadFileTail(string path, int lineCount, string workingDirectory)
        {
            try
            {
                string validatedPath = ValidatePath(path, workingDirectory);
                return _inner.ReadFileTail(validatedPath, lineCount, "");
            }
            catch (Exception ex)
            {
                _ledger.RecordAction($"ReadFileTail: {path} (lines {lineCount})", false, ex.Message);
                throw;
            }
        }

        public string Grep(string pattern, string path, string workingDirectory)
        {
            try
            {
                string validatedPath = ValidatePath(path, workingDirectory);
                return _inner.Grep(pattern, validatedPath, "");
            }
            catch (Exception ex)
            {
                _ledger.RecordAction($"Grep: {path} with pattern {pattern}", false, ex.Message);
                throw;
            }
        }

        public string WriteFile(string path, string content, string workingDirectory)
        {
            try
            {
                string validatedPath = ValidatePath(path, workingDirectory);
                
                string result = _inner.WriteFile(validatedPath, content, "");
                _ledger.RecordAction($"WriteFile: {validatedPath}", true, result);
                return result;
            }
            catch (Exception ex)
            {
                _ledger.RecordAction($"WriteFile: {path}", false, ex.Message);
                throw;
            }
        }

        public bool FileExists(string path, string workingDirectory)
        {
            try
            {
                string validatedPath = ValidatePath(path, workingDirectory);
                return _inner.FileExists(validatedPath, "");
            }
            catch (Exception ex)
            {
                _ledger.RecordAction($"FileExists: {path}", false, ex.Message);
                return false;
            }
        }

        public string GetFileList(bool recursive, string searchPattern, string workingDirectory)
        {
            try
            {
                string validatedPath = ValidatePath(".", workingDirectory);
                return _inner.GetFileList(recursive, searchPattern, validatedPath);
            }
            catch (Exception ex)
            {
                _ledger.RecordAction($"GetFileList: {workingDirectory}", false, ex.Message);
                throw;
            }
        }

        public string ReadFileWithSearch(string path, string searchTerm, int contextLines, string workingDirectory)
        {
            try
            {
                string validatedPath = ValidatePath(path, workingDirectory);
                string result = _inner.ReadFileWithSearch(validatedPath, searchTerm, contextLines, "");
                _ledger.RecordAction($"ReadFileWithSearch: {validatedPath} (search: {searchTerm})", true, "Search completed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _ledger.RecordAction($"ReadFileWithSearch: {path}", false, ex.Message);
                throw;
            }
        }

        public void WriteFileRange(string path, int startLine, int endLine, string content, FileWriteMode mode, string workingDirectory)
        {
            try
            {
                string validatedPath = ValidatePath(path, workingDirectory);

                // Medium Risk: Surgical editing should be gated
                var manifest = _manifestService.CreateManifest(
                    "WriteFileRange", 
                    RiskLevel.Medium, 
                    $"Surgical edit ({mode}) on lines {startLine}-{endLine} in {validatedPath}", 
                    validatedPath);

                if (!_manifestService.RequestApproval(manifest, _preferences.ActiveProfile))
                {
                    throw new UnauthorizedAccessException($"Approval required for WriteFileRange: {validatedPath}");
                }

                _inner.WriteFileRange(validatedPath, startLine, endLine, content, mode, "");
                _ledger.RecordAction($"WriteFileRange: {validatedPath} (lines {startLine}-{endLine}, mode: {mode})", true, "Surgical edit completed successfully");
            }
            catch (Exception ex)
            {
                _ledger.RecordAction($"WriteFileRange: {path}", false, ex.Message);
                throw;
            }
        }
    }
}
