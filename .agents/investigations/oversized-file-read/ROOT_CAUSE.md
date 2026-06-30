# Investigation Report: Oversized File Read Context Overflow

## Issue Description
The agent can read a file larger than its context window. Once an oversized file is added to the context, all subsequent LLM requests fail due to context overflow. The system cannot self-heal via compaction because the compaction process itself requires an LLM call, which fails when the context is already over the limit.

## Root Cause Analysis

### 1. Post-Facto Compaction Trigger
In `Orchestrator.RunAgentLoopAsync`, the system adds tool results to the conversation history *before* checking if compaction is necessary. 
- **Flow:** `ExecuteTool` $\rightarrow$ `AddMessage` $\rightarrow$ `ShouldCompact`.
- **Result:** If a tool returns a massive amount of data, the context is already oversized before the compaction trigger is even evaluated.

### 2. The Compaction Paradox
The `ConversationManager.CompactAsync` method attempts to resolve overflow by sending the existing history to the LLM for summarization.
- **Failure Point:** Since the LLM is the mechanism for compaction, if the context is already beyond the LLM's hard limit, the request to "summarize the history" will be rejected by the LLM provider with a context overflow error.
- **Impact:** This creates a deadlock where the system cannot recover without external manual intervention (e.g., restarting the session).

### 3. Lack of Input Validation/Truncation
The `ToolManager` and `FileSystem` services do not perform any size checks or truncation on file contents before returning them to the `Orchestrator`. There is no "safe read" mechanism to ensure that a single tool output cannot crash the entire session.

### 4. Subagent Blind Spot
The `RunSubagentLoopAsync` method in `Orchestrator.cs` lacks the compaction logic present in the main loop. Subagents have no mechanism to reduce their context, making them highly fragile when dealing with large files.

## Observed Behaviors
- **Permanent Failure:** When a file exceeds the hard token limit, the agent enters a loop of failed LLM calls.
- **Compaction Thrashing:** When a file is large enough to trigger compaction but small enough to fit in the window, the agent reads the file $\rightarrow$ triggers compaction $\rightarrow$ loses the file content in the summary $\rightarrow$ reads the file again.

## Conclusion
The issue is caused by a combination of "trusting" tool output size and using a "recursive" recovery mechanism (using the LLM to fix LLM overflow) without a fallback or a pre-emptive guard.
