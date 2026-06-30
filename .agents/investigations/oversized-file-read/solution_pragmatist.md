# Pragmatic Solution: Tool Output Safety Valve

## The Problem
A single tool call (e.g., reading a large file) can return a result that exceeds the LLM's context window. Because the system adds the result to the conversation *before* checking for compaction, and because the compaction process itself requires an LLM call, the system enters a deadlock state (The Compaction Paradox) where it cannot recover without a restart.

## The Pragmatic Solution: "The Safety Valve"
Instead of over-engineering a complex streaming reader or a token-aware truncation system, we implement a simple **character-based truncation guardrail** on all tool outputs.

### Core Idea
Any string returned by a tool is passed through a "Safety Valve" that truncates the content if it exceeds a safe threshold (e.g., 20,000 characters) and appends a clear warning message.

### Why this is Pragmatic
- **Low Effort:** Requires only a few lines of code in the `Orchestrator`.
- **High Impact:** Completely eliminates the "Permanent Failure" state. The agent may get a partial file, but the session remains alive and functional.
- **Avoids Over-engineering:** Uses character counts as a "good enough" proxy for tokens, avoiding the overhead of calling the token helper for every single tool output.
- **Breaks the Paradox:** By preventing the context from ever exceeding the hard limit, the existing `CompactAsync` logic remains viable and can trigger normally.

## Proposed Implementation

### 1. Add a Helper Method in `Orchestrator.cs`
```csharp
private string ApplySafetyValve(string input)
{
    const int MaxChars = 20000; // Approx 5k tokens; safe margin for 12.5k limit
    if (string.IsNullOrEmpty(input) || input.Length <= MaxChars) return input;

    return input.Substring(0, MaxChars) + "\n\n[WARNING: Output truncated due to size limit to prevent context overflow]";
}
```

### 2. Wrap Tool Results
Apply this helper to all tool results before they are added to the `ConversationManager`:

**In `RunAgentLoopAsync` (Main Loop):**
```csharp
// For subagents
string result = await HandleSubagentDispatchAsync(toolCall.Function.Arguments, workingDirectory);
_conversationManager.AddMessage(new Message("tool", ApplySafetyValve(result), toolCall.Id));

// For standard tools
string result = _toolManager.ExecuteTool(toolCall.Function.Name, toolCall.Function.Arguments, workingDirectory, permissions);
_conversationManager.AddMessage(new Message("tool", ApplySafetyValve(result), toolCall.Id));
```

**In `RunSubagentLoopAsync` (Subagent Loop):**
```csharp
string result = _toolManager.ExecuteTool(toolCall.Function.Name, toolCall.Function.Arguments, workingDirectory, permissions);
history.Add(new Message("tool", ApplySafetyValve(result), toolCall.Id));
```

## Expected Outcome
1. **No more deadlocks:** No single file read can crash the session.
2. **Graceful Degradation:** The AI is explicitly told when data is truncated, allowing it to potentially try reading the file in parts (if the AI is smart enough) or focus on the provided snippet.
3. **Stability:** The `ConversationManager`'s compaction logic will now work as intended because it will never be called on a context that is already "broken."
