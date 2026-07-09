namespace CodeMonkey.Core.Utility
{
    public static class ContextConstants
    {
        /// <summary>
        /// Hard limit for a single tool output before truncation.
        /// Roughly 4000 tokens.
        /// </summary>
        public const int MaxToolOutputTokens = 4000;

        /// <summary>
        /// Threshold that triggers pre-emptive compaction.
        /// Set to 10,000 to provide a safe buffer (5,000 tokens) 
        /// before hitting the TotalTokenLimit, accommodating a full MaxToolOutputTokens.
        /// </summary>
        public const int SoftLimitTokens = 10000;

        /// <summary>
        /// Hard limit for total context.
        /// </summary>
        public const int TotalTokenLimit = 15000;

        /// <summary>
        /// Percentage of context to retain during emergency pruning.
        /// </summary>
        public const double EmergencyPruneThreshold = 0.5;

        public const string TruncationNotice = 
            "[SYSTEM NOTICE: This output was too large and has been truncated. " +
            "To read the remainder, please use 'read_file_range' with specific line numbers or 'grep' to find specific patterns.]";
    }
}
