using System;
using System.Collections.Generic;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using CodeMonkey.Core.Utility;

namespace CodeMonkey.Core.Services
{
    public class SecureFileSystem : IFileSystem
    {
        private readonly IFileSystem _inner;
        private readonly IUserPreferences _preferences;
        private readonly ISessionLedger _ledger;
        private readonly IManifestService _manifestService;

        public SecureFileSystem(IFileSystem inner, IUserPreferences preferences, ISessionLedger ledger, IManifestService manifestService)
        {
            _inner = inner;
            _preferences = preferences;
            _ledger = ledger;
            _manifestService = manifestService;
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

        public string ReadFileChunked(string path, int startLine, int endLine, string workingDirectory)
        {
            try
            {
                string validatedPath = ValidatePath(path, workingDirectory);
                return _inner.ReadFileChunked(validatedPath, startLine, endLine, "");
            }
            catch (Exception ex)
            {
                _ledger.RecordAction($"ReadFileChunked: {path} (lines {startLine}-{endLine})", false, ex.Message);
                throw;
            }
        }

        public string WriteFile(string path, string content, string workingDirectory)
        {
            try
            {
                string validatedPath = ValidatePath(path, workingDirectory);
                
                // High Risk: File writing should be gated
                var manifest = _manifestService.CreateManifest("WriteFile", RiskLevel.Medium, $"Write content to {validatedPath}", validatedPath);
                if (!_manifestService.RequestApproval(manifest, _preferences.ActiveProfile))
                {
                    return $"Pending approval for WriteFile: {validatedPath}";
                }

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
    }
}
