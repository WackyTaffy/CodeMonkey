# Solution Proposal: Token Efficiency and Context Stability (The Optimizer)

## 1. Overview
The current system is vulnerable to "Context Collapse" where a single oversized file read exceeds the LLM's hard token limit, rendering the agent unresponsive and making LLM-based compaction impossible. 

The proposed solution shifts the strategy from **reactive recovery** (compacting after overflow) to **pre-emptive prevention** (filtering and limiting before injection) and introduces a **fail-safe mechanism** for emergency recovery.

---

## 2. Pre-emptive Guardrails (Input Filtering)

### 2.1. Tool Output Token Capping
Instead of blindly adding tool results to the conversation, the `ToolManager` or `Orchestrator` must enforce a hard limit on the size of a single tool response.

- **Implementation**: Introduce a `MaxToolOutputTokens` constant (e.g., 4,000 tokens).
- **Logic**: If a tool result (like `ReadFile`) exceeds this limit, the system should:
    1. Truncate the content.
    2. Append a metadata warning: `[CONTENT TRUNCATED: File too large. Please use specific line ranges or request a summary if needed.]`
    3. Return only the truncated version.

### 2.2. Intelligent "Safe Read" Mechanism
Modify the file reading tool to support partial reads.
- **Line-Based Reading**: Instead of `ReadFile(path)`, introduce `ReadFileLines(path, startLine, endLine)`.
- **Header/Footer Preview**: For files exceeding the token cap, automatically provide the first 100 lines and last 100 lines, skipping the middle, to provide context without overloading.

---

## 3. Smarter Compaction Triggers

### 3.1. Pre-Injection Validation
The `Orchestrator` currently adds the message and *then* checks if it should compact. This is the primary cause of the "Compaction Paradox."

**Proposed Flow Change:**
`ExecuteTool` $\rightarrow$ `CalculatePotentialTokenCount` $\rightarrow$ `(If > Limit) CompactAsync` $\rightarrow$ `AddMessage`.

By compacting *before* adding a large tool result, we ensure the LLM call for compaction happens while the context is still within the operational window.

### 3.2. Non-LLM Emergency Fallback (The "Kill-Switch")
To solve the deadlock where the LLM cannot be called to summarize the history, implement a **Deterministic Compactor**.

- **Trigger**: If `GetTotalTokenCount()` exceeds the hard limit of the LLM provider.
- **Mechanism**: 
    1. Preserve the `System Prompt`.
    2. Preserve the `Last User Request`.
    3. Drop the oldest `tool` and `ai` messages in chronological order until the token count is reduced to 50% of the limit.
    4. Add a system message: `[EMERGENCY CONTEXT PURGE: Oldest history was removed to prevent system failure.]`

---

## 4. Subagent Parity & Efficiency

### 4.1. Subagent Compaction
Subagents currently operate without compaction, making them "brittle."
- **Implementation**: Integrate the same `ShouldCompact` $\rightarrow$ `CompactAsync` loop into `RunSubagentLoopAsync`.

### 4.2. Initial Context Optimization
The `HandleSubagentDispatchAsync` method currently reads all initial context files fully.
- **Optimization**: Apply the same "Safe Read" / "Token Capping" logic to the `Initial Context` builder to prevent subagents from starting in an overflowed state.

---

## 5. Summary of Proposed Changes

| Feature | Current State | Proposed State | Impact |
| :--- | :--- | :--- | :--- |
| **Tool Output** | Unlimited | Capped at $N$ tokens + Warning | Prevents single-call overflow |
| **Compaction Trigger** | Post-addition | Pre-addition | Solves Compaction Paradox |
| **Recovery** | LLM-only (Recursive) | LLM $\rightarrow$ Deterministic Fallback | Eliminates permanent deadlocks |
| **Subagents** | No compaction | Full compaction lifecycle | Increases subagent reliability |
| **File Reading** | Full read only | Range-based / Header-Footer | Higher token efficiency |
