# Unified Solution: Oversized File Read and Context Overflow

## 1. Problem Statement: The "Context Deadlock"
The system currently suffers from a catastrophic failure state known as the **Compaction Paradox**. When a tool (primarily file reading) returns a payload that exceeds the LLM's hard context limit, the session becomes unrecoverable. Because the current compaction mechanism relies on the LLM to summarize history, a context overflow prevents the very mechanism designed to fix it from executing, leading to a permanent deadlock loop.

## 2. Unified Strategy: Layered Defense
To eliminate this issue, we will implement a multi-layered defense strategy that shifts the system from **reactive recovery** to **pre-emptive prevention** and **deterministic recovery**.

### Layer 1: Preventative Guardrails (The Input Guard)
**Goal:** Prevent oversized payloads from ever entering the conversation history.

- **Hard Tool Output Limits:** Implement a `MaxToolOutputTokens` (or a character-based proxy for performance) limit. Any tool output exceeding this limit is automatically truncated.
- **Truncation Notice:** When truncation occurs, append a clear system notice: 
  `[SYSTEM NOTICE: This output was too large and has been truncated. To read the remainder, please use 'read_file_range' with specific line numbers or 'grep' to find specific patterns.]`
- **Surgical Read Tooling:** Replace/augment "Read All" behavior with targeted tools:
    - `read_file_range(path, startLine, endLine)`: Allows the agent to paginate through large files.
    - `grep(path, pattern)`: Allows the agent to identify relevant line ranges.
    - `read_file_head/tail(path, count)`: Quick inspection of file boundaries.

### Layer 2: Pre-emptive Orchestration (The Admission Controller)
**Goal:** Solve the "Compaction Paradox" by ensuring the context is healthy *before* adding new data.

- **Reversed Logic Flow:** Modify `Orchestrator.cs` to change the order of operations:
    - **Current:** `ExecuteTool` $\rightarrow$ `AddMessage` $\rightarrow$ `ShouldCompact`.
    - **Proposed:** `ExecuteTool` $\rightarrow$ `CalculateProspectiveSize` $\rightarrow$ `(If > SoftLimit) CompactAsync` $\rightarrow$ `AddMessage`.
- **Admission Control:** The Orchestrator must validate the size of a result against the hard limit *before* committing it to the `ConversationManager`.

### Layer 3: Emergency Recovery (The Safety Valve)
**Goal:** Provide a non-LLM dependent "emergency exit" to break deadlocks.

- **Deterministic Pruning (Non-Recursive):** If an LLM call fails with a `ContextOverflowException` (even during compaction), the system triggers a **Hard Reset**:
    1. **Preserve:** System Prompt and the most recent User/AI exchange.
    2. **Discard:** All intermediate tool outputs and history in a FIFO (First-In, First-Out) manner until the context is reduced to 50% of the hard limit.
    3. **Notify:** Add a system message: `[SYSTEM]: Emergency context pruning performed due to critical overflow. Some historical context has been lost.`

### Layer 4: System-wide Consistency (Unified Agent Loop)
**Goal:** Ensure subagents are not "blind spots" in the safety regime.

- **Unified Loop:** Abstract the agent loop into a shared engine so that both Main Agents and Subagents inherit the same Input Guard, Admission Control, and Emergency Recovery logic.
- **Initial Context Filtering:** Apply the same truncation and safety limits to the initial context files provided to subagents to prevent them from starting in an overflowed state.

---

## 3. Implementation Roadmap

### Phase 1: Immediate Mitigation (The "Pragmatist" Phase)
- Implement a simple character-based truncation helper in `Orchestrator.cs`.
- Wrap all tool outputs in this helper to prevent immediate session crashes.

### Phase 2: Tooling & Prevention (The "UX/Safety" Phase)
- Implement `read_file_range` and `grep` in `FileSystem` and `ToolManager`.
- Update the `read_file` tool to return a "Too Large" error with instructions instead of the full content when a threshold is exceeded.

### Phase 3: Orchestration Refactor (The "Architect" Phase)
- Refactor the `Orchestrator` loop to implement Pre-emptive Compaction.
- Implement the deterministic `EmergencyPruning` logic in `ConversationManager`.
- Unify the Main and Subagent loops to ensure consistent safety.

## 4. Impact Summary

| Root Cause | Unified Solution Component | Outcome |
| :--- | :--- | :--- |
| **Post-Facto Compaction** | Pre-emptive Orchestration (Layer 2) | Context is cleared *before* the overflow occurs. |
| **Compaction Paradox** | Emergency Recovery (Layer 3) | System recovers via deterministic purge without needing LLM. |
| **Lack of Validation** | Input Guard (Layer 1) | Massive files are truncated and flagged; surgical tools provided. |
| **Subagent Blind Spot** | Unified Agent Loop (Layer 4) | Subagents gain the same stability as the main agent. |
| **Compaction Thrashing** | Truncation Notice (Layer 1) | Agent is explicitly told why data is missing, preventing blind re-reads. |
