# Safety Engineering Solution: Preventing Context Overflow and the Compaction Paradox

## 1. Overview
The current system is vulnerable to a "catastrophic failure" state where a single oversized file read can exceed the LLM's hard token limit. This creates the **Compaction Paradox**: the system attempts to use the LLM to summarize the context to fix the overflow, but the LLM rejects the request because the context is already too large.

This solution implements a multi-layered defense strategy: **Preventative Guardrails**, **Pre-emptive Validation**, and a **Non-Recursive Safety Valve**.

---

## 2. Layer 1: Preventative Guardrails (Input Validation)
The first line of defense is to ensure that no single tool output can ever be large enough to crash the session.

### A. Tool Output Truncation
Implement a hard limit on the size of strings returned by the `IToolManager` and `IFileSystem`.
- **Maximum Tool Output Limit:** Set a constant (e.g., `MAX_TOOL_OUTPUT_TOKENS = 5000`).
- **Automatic Truncation:** If a file or command output exceeds this limit, the system must truncate the content and append a warning: 
  > `[SAFETY WARNING]: The output was too large and has been truncated to fit within the safety window. Please use targeted reads (e.g., read specific lines) if more data is needed.`

### B. "Safe Read" Capability
Modify the `ReadFile` tool to support range-based reading (e.g., `read_file(path, start_line, end_line)`). This encourages the AI to paginate through large files rather than attempting to swallow them whole.

---

## 3. Layer 2: Pre-emptive Context Management
Currently, the `Orchestrator` adds tool results to the context *before* checking if compaction is needed. This must be reversed.

### A. The "Admission Control" Pattern
The `Orchestrator` should validate the size of a tool's result *before* it is committed to the `ConversationManager`.

**Proposed Logic:**
1. Tool executes and returns `result`.
2. Orchestrator calculates: `PotentialNewSize = CurrentContextSize + TokenCount(result)`.
3. If `PotentialNewSize > HardLimit`:
   - Truncate `result` to a safe size.
   - Log a safety warning.
4. Add `result` to `ConversationManager`.
5. Trigger `CompactAsync` if `PotentialNewSize > SoftLimit`.

---

## 4. Layer 3: Solving the Compaction Paradox (The Safety Valve)
To prevent the deadlock where the LLM cannot summarize its own overflow, we must implement a **Non-Recursive Fallback**.

### A. Emergency Pruning (The Nuclear Option)
If `CompactAsync` (the LLM-based summary) fails due to a context overflow error from the API, the system must switch to a deterministic, non-LLM pruning method.

**Emergency Pruning Algorithm:**
1. **Preserve:** Keep the `System Prompt`.
2. **Preserve:** Keep the most recent `User` and `AI` exchange (the current turn).
3. **Discard:** Delete the oldest messages in the history (FIFO) until the total token count is below 50% of the hard limit.
4. **Notify:** Add a system message: `[SYSTEM]: Emergency context pruning performed due to critical overflow. Some historical context has been lost.`

This ensures the system can always "self-heal" regardless of the LLM provider's state.

---

## 5. Layer 4: Subagent Safety Standardization
Subagents are currently "blind" to context limits. They must be brought under the same safety regime as the Main Agent.

1. **Inherit Guards:** The `RunSubagentLoopAsync` must use the same `Admission Control` logic for tool outputs.
2. **Enable Compaction:** Subagents must have access to `CompactAsync` and the `Emergency Pruning` fallback.
3. **Strict Output Limits:** Since subagents return their final result to the Main Agent, their final response must also be subject to truncation to prevent the subagent from crashing the main orchestrator.

## Summary of Safety Limits

| Component | Guard | Action on Violation |
| :--- | :--- | :--- |
| **Tool Output** | `MAX_TOOL_OUTPUT_TOKENS` | Truncate + Warning |
| **Context Entry** | Admission Control | Truncate before `AddMessage` |
| **Compaction** | LLM Summarization | Trigger on `SoftLimit` |
| **Recovery** | Emergency Pruning | FIFO Purge on LLM Failure |
