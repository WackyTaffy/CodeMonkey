using Microsoft.ML.Tokenizers;
using CodeMonkey.Core.Interfaces;

namespace CodeMonkey.Core.Utility
{
    public class GemmaTokenHelper : ITokenHelper
    {
        private SentencePieceTokenizer? _tokenizer;
        private readonly object _lock = new();

        private const string _modelPath = @"D:\Models\tokenizer.model";

        /// <summary>
        /// Initializes the tokenizer with the Gemma tokenizer.model file.
        /// Call this once during application startup.
        /// </summary>
        /// <param name="modelPath">Path to the downloaded tokenizer.model file.</param>
        public GemmaTokenHelper()
        {
            if (_tokenizer != null) return;

            lock (_lock)
            {
                if (_tokenizer != null) return;

                if (!File.Exists(_modelPath))
                {
                    throw new FileNotFoundException("Gemma tokenizer.model file not found.", _modelPath);
                }

                using var modelStream = File.OpenRead(_modelPath);

                // Gemma models typically expect a Beginning of Sentence () token
                _tokenizer = SentencePieceTokenizer.Create(modelStream, addBeginningOfSentence: true);
            }
        }

        /// <summary>
        /// Gets the exact token count for the given text. 
        /// Falls back to a heuristic estimate if the tokenizer is not initialized.
        /// </summary>
        public int GetTokenCount(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            // Exact count using the loaded model
            if (_tokenizer != null)
            {
                return _tokenizer.CountTokens(text);
            }

            // Fallback estimate (approx. 4 characters per token for Gemma/English)
            return (int)Math.Ceiling(text.Length / 4.0);
        }
    }
}
