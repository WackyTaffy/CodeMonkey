# Architectural Solution: Oversized File Read Context Overflow

## 1. Problem Statement
The system currently suffers from a "Context Deadlock." When a tool (specifically file reading) returns a payload larger than the LLM's hard context limit, the session becomes unrecoverable. Because the current compaction mechanism relies on the LLM itself to summarize history, a context overflow prevents the very mechanism designed to fix it from executing.

## 2. Architectural Goals
- **Prevention**: Prevent oversized payloads from ever entering the conversation history.
- **Resilience**: Provide a non-LLM dependent "emergency recovery" path to break deadlocks.
- **Scalability**: Implement a pattern for handling large data that allows the agent to explore files incrementally.
- **Consistency**: Ensure all agents (Main and Subagents) adhere to the same safety constraints.

---

## 3. Proposed Solution: The "Safe-Context" Framework

I propose a multi-layered defense strategy based on the **Interceptor** and **Circuit Breaker** design patterns.

### Layer 1: The Input Guard (Prevention)
Instead of blindly adding tool output to the context, we introduce a `ContextGuard` interceptor.

**Design Pattern: Interceptor / Strategy**
- **Max Payload Limit**: Define a `HardMaxToolOutputTokens` (e.g., 4,000 tokens).
- **Truncation Strategy**: If a tool output exceeds this limit, the `ContextGuard` will:
    1. Truncate the content.
    2. Append a "Truncation Notice" metadata block.
    3. **Example Notice**: `[SYSTEM NOTICE: This file is too large to fit in context. Only the first 4,000 tokens are shown. To read more, use 'read_file_chunked' with start/end line numbers.]`
- **New Tool**: Introduce `read_file_chunked(path, startLine, endLine)`. This transforms the agent's behavior from "Read Whole File" $\rightarrow$ "Explore File via Windowing."

### Layer 2: Pre-emptive Compaction (Timing)
Correct the logic in `Orchestrator.cs` to move the compaction check *before* the addition of potentially large data.

**Proposed Logic Flow**:
1. Execute Tool.
2. Calculate prospective token count: `CurrentTokens + ToolResultTokens`.
3. If `ProspectiveTokens > TokenLimit`:
    - Trigger `CompactAsync` **before** adding the tool result.
    - If `CompactAsync` fails or is impossible, fall back to Layer 3.
4. Add Tool Result to history.

### Layer 3: The Emergency Safety Valve (Recovery)
To solve the "Compaction Paradox," we need a recovery mechanism that does *not* require an LLM call.

**Design Pattern: Circuit Breaker**
- **Detection**: Catch `ContextOverflowException` (or equivalent provider error) during `GetChatCompletionAsync`.
- **Action (Hard Reset)**: If a deadlock is detected, the `ConversationManager` executes an `EmergencyPurge()`:
    - Keep: System Prompt + Last User Message.
    - Discard: All intermediate tool outputs and AI thoughts.
    - Result: The context is instantly reduced to a minimum viable state, allowing the agent to resume and "realize" it lost its data, prompting it to read the file again (this time, subject to Layer 1 truncation).

### Layer 4: Unified Agent Loop (Consistency)
Abstract the agent loop into a shared `AgentEngine` or `BaseAgentLoop` class.

- Currently, `RunAgentLoopAsync` and `RunSubagentLoopAsync` are separate implementations.
- By unifying them, both the Main Agent and Subagents automatically inherit the `ContextGuard`, Pre-emptive Compaction, and Emergency Recovery logic.

---

## 4. Implementation Roadmap

### Phase 1: Infrastructure (The Guard)
- [ ] Create `IContextGuard` service.
- [ ] Implement token-based truncation in `IContextGuard`.
- [ ] Add `read_file_chunked` to `FileSystem` and `ToolManager`.

### Phase 2: Orchestration (The Loop)
- [ ] Refactor `Orchestrator.cs` to use a unified loop.
- [ ] Move `ShouldCompact` check to occur *before* `AddMessage` for tool results.
- [ ] Integrate `IContextGuard` into the tool execution pipeline.

### Phase 3: Resilience (The Valve)
- [ ] Update `GetResponseWithRetryAsync` to detect context overflow errors.
- [ ] Implement `ConversationManager.EmergencyPurge()`.
- [ ] Wire the overflow error to trigger the purge.

## 5. Impact Analysis

| Root Cause | Solution Component | Result |
| :--- | :--- | :--- |
| **Post-Facto Compaction** | Pre-emptive Compaction (Layer 2) | Context is cleared *before* the overflow occurs. |
| **Compaction Paradox** | Emergency Safety Valve (Layer 3) | System recovers without LLM intervention via hard-purge. |
| **Lack of Validation** | Input Guard (Layer 1) | Massive files are truncated and flagged; "Chunked Read" provided. |
| **Subagent Blind Spot** | Unified Agent Loop (Layer 4) | Subagents gain all safety and recovery mechanisms. |
| **Compaction Thrashing** | Truncation Notice (Layer 1) | Agent is explicitly told *why* data is missing, preventing blind re-reads. |
