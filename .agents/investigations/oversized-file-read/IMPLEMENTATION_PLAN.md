# Implementation Plan: Oversized File Read and Context Overflow

This document outlines the phased implementation of the unified solution to prevent and recover from context overflow caused by large tool outputs.

## Goals
- Prevent session-killing context overflows (The "Compaction Paradox").
- Provide surgical tools for large file inspection.
- Ensure deterministic recovery when overflows occur.
- Unify safety mechanisms across main agents and subagents.

---

## Phase 1: Immediate Mitigation (Input Guard)
**Objective:** Prevent oversized payloads from crashing the session by implementing hard limits and truncation.

### 1.1 Define Safety Thresholds
- [ ] Create a configuration or constants file (e.g., in `CodeMonkey.Core`) to define:
    - `MaxToolOutputCharacters`: Hard limit for a single tool output.
    - `SoftLimitCharacters`: Threshold that triggers pre-emptive compaction.
    - `EmergencyPruneThreshold`: Percentage (e.g., 50%) of context to retain during emergency pruning.

### 1.2 Implement Truncation Logic
- [ ] Create a utility method/service to handle content truncation.
- [ ] Ensure that if truncation occurs, the following notice is appended:
  `[SYSTEM NOTICE: This output was too large and has been truncated. To read the remainder, please use 'read_file_range' with specific line numbers or 'grep' to find specific patterns.]`

### 1.3 Integrate Truncation in Orchestrator
- [ ] Modify `Orchestrator.cs` to wrap all tool outputs with the truncation utility before they are passed to the `ConversationManager`.

### 1.4 Testing & Documentation
- [ ] **Unit Testing**: 
    - Create tests for the truncation utility to verify it correctly truncates at the limit and appends the notice.
    - Create tests for the `Orchestrator` integration to ensure truncation is applied to tool results.
- [ ] **Documentation**: 
    - Create `docs/context-management.md` to document the safety thresholds, the truncation strategy, and the rationale behind the chosen limits.

**Verification:**
- [ ] Attempt to read a file larger than `MaxToolOutputCharacters`.
- [ ] Verify the output is truncated and the system notice is present.
- [ ] Verify the session remains responsive.

---

## Phase 2: Surgical Tooling (Prevention)
**Objective:** Provide the agent with the means to read large files without needing "Read All" behavior.

### 2.1 Implement Targeted File Tools
- [ ] Implement `read_file_range(path, startLine, endLine)` in the file system service.
- [ ] Implement `grep(path, pattern)` in the file system service.
- [ ] Implement `read_file_head(path, count)` and `read_file_tail(path, count)` in the file system service.

### 2.2 Update Tool Registration
- [ ] Register these new tools in `ToolManager` so they are available to the LLM.

### 2.3 Refactor `read_file`
- [ ] Modify `read_file` to check the file size *before* reading.
- [ ] If the file exceeds a specific threshold, return a "Too Large" error message instead of the content, suggesting the use of the new surgical tools.

### 2.4 Testing & Documentation
- [ ] **Unit Testing**: 
    - Create unit tests for each new tool (`read_file_range`, `grep`, `head`, `tail`) with various file sizes and edge cases (e.g., out-of-bounds lines).
    - Create an integration test for `read_file` to verify it returns the "Too Large" error when the threshold is exceeded.
- [ ] **Documentation**: 
    - Update the project's tool reference documentation (e.g., `docs/tools.md` or similar) to include detailed descriptions, parameters, and usage examples for the new surgical tools.

**Verification:**
- [ ] Use `grep` to find a pattern in a large file.
- [ ] Use `read_file_range` to read the specific lines found by `grep`.
- [ ] Verify that `read_file` on a massive file returns the suggested tool error instead of attempting to load the whole file.

---

## Phase 3: Pre-emptive Orchestration (Admission Control)
**Objective:** Solve the Compaction Paradox by cleaning context *before* adding new data.

### 3.1 Implement Size Calculation
- [ ] Implement `CalculateProspectiveSize(newMessage)` in `ConversationManager` to estimate the total context size if a message were to be added.

### 3.2 Refactor Orchestrator Loop
- [ ] Change the execution flow in `Orchestrator.cs`:
    - **Current:** `ExecuteTool` $\rightarrow$ `AddMessage` $\rightarrow$ `ShouldCompact`.
    - **New:** `ExecuteTool` $\rightarrow$ `CalculateProspectiveSize` $\rightarrow$ `(If > SoftLimit) CompactAsync` $\rightarrow$ `AddMessage`.

### 3.3 Admission Validation
- [ ] Implement a final check in the `Orchestrator` to ensure the tool result does not exceed the hard limit before committing it to the `ConversationManager`.

### 3.4 Testing & Documentation
- [ ] **Unit Testing**: 
    - Create unit tests for `CalculateProspectiveSize` to ensure accuracy.
    - Create integration tests for the `Orchestrator` loop to verify that `CompactAsync` is called *before* a message is added when the soft limit is reached.
- [ ] **Documentation**: 
    - Update `docs/architecture.md` to describe the change in the orchestrator loop and the concept of "Admission Control."

**Verification:**
- [ ] Trigger a scenario where a tool output is large enough to hit the `SoftLimit`.
- [ ] Verify that `CompactAsync` is called *before* the tool output is added to the history.

---

## Phase 4: Emergency Recovery (The Safety Valve)
**Objective:** Provide a non-LLM dependent recovery path to break deadlock loops.

### 4.1 Implement Deterministic Pruning
- [ ] Implement `EmergencyPruning()` in `ConversationManager`:
    - Keep the System Prompt.
    - Keep the most recent User and AI messages.
    - Discard older messages in FIFO order until total size is $\le$ `EmergencyPruneThreshold`.

### 4.2 Implement Overflow Interception
- [ ] Wrap LLM calls in `Orchestrator` with a try-catch block.
- [ ] On `ContextOverflowException` (or equivalent API error), trigger `EmergencyPruning()`.
- [ ] After pruning, inject a system message: `[SYSTEM]: Emergency context pruning performed due to critical overflow. Some historical context has been lost.`

### 4.3 Testing & Documentation
- [ ] **Unit Testing**: 
    - Create unit tests for `EmergencyPruning()` to verify that the correct messages are preserved and discarded.
    - Implement a mock LLM failure to trigger the `ContextOverflowException` and verify the recovery flow in the `Orchestrator`.
- [ ] **Documentation**: 
    - Update `docs/context-management.md` to explain the "Emergency Recovery" mechanism and how it breaks the Compaction Paradox.

**Verification:**
- [ ] Force a context overflow (e.g., by bypassing limits or using a very small limit).
- [ ] Verify the system automatically prunes the context and continues without crashing.
- [ ] Verify the emergency notification is visible in the history.

---

## Phase 5: System-wide Consistency (Unified Loop)
**Objective:** Ensure subagents inherit all safety guards.

### 5.1 Abstract Agent Engine
- [ ] Identify the common loop logic between Main Agents and Subagents.
- [ ] Extract this logic into a shared `AgentEngine` class/module.

### 5.2 Integrate Guards into Shared Engine
- [ ] Ensure the `AgentEngine` implements:
    - Truncation (Phase 1).
    - Pre-emptive Compaction (Phase 3).
    - Emergency Pruning (Phase 4).

### 5.3 Subagent Context Filtering
- [ ] Apply truncation and safety limits to the "Initial Context" files provided to subagents during dispatch to prevent them from starting in an overflow state.

### 5.4 Testing & Documentation
- [ ] **Unit Testing**: 
    - Verify the `AgentEngine` correctly applies all guards across different agent types.
    - Test the dispatch of subagents with oversized initial context to verify filtering.
- [ ] **Documentation**: 
    - Update `docs/architecture.md` to document the new `AgentEngine` and how it provides consistent stability across the system.

**Verification:**
- [ ] Dispatch a subagent with a massive initial context.
- [ ] Verify the subagent's context is truncated/filtered.
- [ ] Verify the subagent can use surgical tools and recovers from overflows via the shared engine.
