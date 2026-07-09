using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Utility;

namespace CodeMonkey.Core.Services
{
    public interface IContextGuard
    {
        string Guard(string input, int maxTokens);
        int GetTokenCount(string text);
    }

    public class ContextGuard : IContextGuard
    {
        private readonly ITokenHelper _tokenHelper;

        public ContextGuard(ITokenHelper tokenHelper)
        {
            _tokenHelper = tokenHelper;
        }

        public string Guard(string input, int maxTokens)
        {
            if (string.IsNullOrEmpty(input)) return input;

            int currentTokens = _tokenHelper.GetTokenCount(input);
            if (currentTokens <= maxTokens)
            {
                return input;
            }

            // Truncate by character approximation since we can't easily "slice" tokens without a decoder
            // 4 chars per token is a rough estimate.
            int approxCharsToKeep = maxTokens * 4;
            string truncated = input.Length <= approxCharsToKeep ? input : input.Substring(0, approxCharsToKeep);
            
            return $"{truncated}\n\n{ContextConstants.TruncationNotice}";
        }

        public int GetTokenCount(string text)
        {
            return _tokenHelper.GetTokenCount(text);
        }
    }
}
