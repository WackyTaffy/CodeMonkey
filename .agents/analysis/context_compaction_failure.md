# Root Cause Analysis: Context Compaction Failure

## Issue
The CodeMonkey application can enter a state where the context window exceeds the token limit (e.g., > 15k tokens), but the compaction mechanism fails to reduce it, leading to potential LLM failures or performance degradation.

## Root Cause
The failure is caused by a logic mismatch between how compaction is **triggered** and how it is **executed**.

### 1. Trigger Mechanism
In `Orchestrator.cs`, compaction is triggered solely based on the total token count:
```csharp
private const int TokenLimit = 12500;
// ...
if (_conversationManager.ShouldCompact(TokenLimit))
{
    await CompactContextAsync(workingDirectory);
}
```

### 2. Execution Constraints
In `ConversationManager.cs`, the `CompactAsync` method has strict guard clauses regarding the number of messages:
```csharp
public async Task<string> CompactAsync(ILLMClient llmClient, string systemPrompt)
{
    // Guard 1: Minimum message count
    if (_messages.Count < 4) return "FAILED: Message count under threshhold for compaction";

    // ... (logic to identify messages to summarize) ...

    // Guard 2: Ensure there are messages to summarize (excluding system and last round)
    if (!messagesToSummarize.Any()) return "FAILED: No messages avaliablie for compaction";
    
    // ...
}
```

### 3. The Failure Scenario
The system fails when a **small number of very large messages** are added to the context. 

**Example Sequence:**
1. **System Prompt**: Added (1 message).
2. **User Request**: "Read the huge file X" (1 message).
3. **Tool Execution**: `read_file` returns a file with 20,000 tokens (1 message).

**State:**
- Total Messages: 3
- Total Tokens: ~20,000+
- `ShouldCompact(12500)` $\rightarrow$ **True**
- `CompactAsync()` $\rightarrow$ Hits `if (_messages.Count < 4)` $\rightarrow$ **Returns "FAILED"**

Because the large message is preserved and the message count remains low, the system enters a loop where it recognizes the context is too large but refuses to compact it because there aren't enough "rounds" of conversation to summarize.

## Conclusion
The compaction logic assumes that high token counts are always the result of a long conversation history. It does not account for the case where the token limit is exceeded by a few high-density messages.
