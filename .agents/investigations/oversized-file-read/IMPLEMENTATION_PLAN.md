# Subagent Dispatch Plan: Resolving the Compaction Paradox

This document specifies the strategic use of subagents to execute the "Layered Defense" strategy against context overflow and the Compaction Paradox. The primary agent (Architect) retains control over the core `Orchestrator` loop and `ConversationManager` logic, while subagents handle auditing, utility implementation, and validation.

## 🎯 Overall Objective
To transition the system from reactive recovery to a layered defense (Input Guard $\rightarrow$ Admission Control $\rightarrow$ Emergency Recovery $\rightarrow$ Unified Loop $\rightarrow$ Surgical Tooling) using a distributed workforce to accelerate implementation and ensure comprehensive coverage.

### Goals
- Prevent session-killing context overflows (The "Compaction Paradox").
- Provide surgical tools for large file inspection.
- Ensure deterministic recovery when overflows occur.
- Unify safety mechanisms across main agents and subagents.

---

## 🤖 Subagent Roles & Responsibilities

### 1. The Auditor (Exploration & Analysis)
**Objective:** 
- Map every single tool in the system that can return variable-length strings.
- Identify "Explosion Points" within those tools.
- **Primary Focus:** Analysis of `ToolManager`, `FileSystem`, and `Shell` services.
- **Expected Output:** A Markdown table listing Tool Name, Potential for Overflow (Low/Med/High), Current return type, and Recommendation for truncation.

### 2. The Toolsmith (Surgical Tooling Implementation)
**Objective:** 
- Implement precision access tools as "Surgical" alternatives to `read_file`.
- Replace "Read All" patterns with these precision access tools.
- **Primary Focus:** `FileSystem.cs` and `IFileSystem.cs`.
- **Expected Output:** 
    - Implementation of `read_file_range`.
    - Implementation of `read_file_head`.
    - Implementation of `read_file_tail`.
    - Implementation of `grep`.

### 3. The QA Engineer (Validation & Stress Testing)
**Objective:** 
- Create "Context Bombs"—unit tests that specifically attempt to trigger the Compaction Paradox.
- Verify that the Safety Valve and Admission Control actually work using these tests.
- **Primary Focus:** `OrchestratorTests.cs`, `Orchestrator.cs`, and `ConversationManager.cs`.
- **Expected Output:** A new test file `CodeMonkey.Tests/ContextSafetyTests.cs` with specific failure-case tests.

### 4. The Validator (Independent Verification)
**Objective:** 
- Act as the "Final Gate" for each phase.
- Independently execute the manual "Verification" checklists to ensure the system behaves as expected in real-world scenarios.
- Verify that the implementation matches the architectural intent (traceability).
- **Primary Focus:** End-to-end system behavior and the "Verification" sections of the roadmap.
- **Expected Output:** A signed-off "Verification Report" for each phase confirming that all verification bullets were tested and passed.

### 5. The Chronicler (Documentation & Knowledge Transfer)
**Objective:** 
- Document the "Layered Defense" logic.
- Document guidelines to prevent future regression.
- **Primary Focus:** `docs/designs/conversation_compaction.md`, `docs/guides/context-management.md`, and `docs/tools.md`.
- **Expected Output:** 
    - Updated design documents.
    - New guides explaining limits and the Safety Valve.

---

## 📋 Detailed Implementation Roadmap

### Phase 1: Immediate Mitigation (Input Guard)
**Objective:** Prevent oversized payloads from crashing the session by implementing hard limits and truncation.

- [x] **1.1 Define Safety Thresholds** `[Architect]`
    - Create a configuration or constants file (e.g., in `CodeMonkey.Core`) to define:
        - `MaxToolOutputCharacters`: Hard limit for a single tool output.
        - `SoftLimitCharacters`: Threshold that triggers pre-emptive compaction.
        - `EmergencyPruneThreshold`: Percentage (e.g., 50%) of context to retain during emergency pruning.
- [x] **1.2 Implement Truncation Logic** `[Architect]`
    - Create a utility method/service to handle content truncation.
    - Ensure that if truncation occurs, the following notice is appended:
      `[SYSTEM NOTICE: This output was too large and has been truncated. To read the remainder, please use 'read_file_range' with specific line numbers or 'grep' to find specific patterns.]`
- [x] **1.3 Integrate Truncation in Orchestrator** `[Architect]`
    - Modify `Orchestrator.cs` to wrap all tool outputs with the truncation utility before they are passed to the `ConversationManager`.
- [x] **1.4 Testing & Documentation**
    - [x] Create tests for the truncation utility to verify it correctly truncates at the limit and appends the notice. `[QA Engineer]`
    - [x] Create tests for the `Orchestrator` integration to ensure truncation is applied to tool results. `[QA Engineer]`
    - [x] Create `docs/context-management.md` to document the safety thresholds, the truncation strategy, and the rationale behind the chosen limits. `[Chronicler]`

**Verification:** `[The Validator]`
- [x] Attempt to read a file larger than `MaxToolOutputCharacters`.
- [x] Verify the output is truncated and the system notice is present.
- [x] Verify the session remains responsive.

---

### Phase 2: Surgical Tooling (Prevention)
**Objective:** Provide the agent with the means to read large files without needing "Read All" behavior.

- [ ] **2.1 Implement Targeted File Tools**
    - [ ] Implement `read_file_range(path, startLine, endLine)` in the file system service. `[Toolsmith]`
    - [ ] Implement `grep(path, pattern)` in the file system service. `[Toolsmith]`
    - [ ] Implement `read_file_head(path, count)` in the file system service. `[Toolsmith]`
    - [ ] Implement `read_file_tail(path, count)` in the file system service. `[Toolsmith]`
- [ ] **2.2 Update Tool Registration** `[Architect]`
    - Register these new tools in `ToolManager` so they are available to the LLM.
- [ ] **2.3 Refactor `read_file`**
    - [ ] Modify `read_file` to check the file size *before* reading. `[Architect / Toolsmith]`
    - [ ] Modify `read_file` to return a "Too Large" error message if the threshold is exceeded, suggesting the use of the new surgical tools. `[Architect / Toolsmith]`
- [ ] **2.4 Testing & Documentation**
    - [ ] Create unit tests for `read_file_range` with various file sizes and edge cases. `[QA Engineer]`
    - [ ] Create unit tests for `grep` with various file sizes and edge cases. `[QA Engineer]`
    - [ ] Create unit tests for `read_file_head` with various file sizes and edge cases. `[QA Engineer]`
    - [ ] Create unit tests for `read_file_tail` with various file sizes and edge cases. `[QA Engineer]`
    - [ ] Create an integration test for `read_file` to verify it returns the "Too Large" error when the threshold is exceeded. `[QA Engineer]`
    - [ ] Update the project's tool reference documentation (e.g., `docs/tools.md` or similar) to include detailed descriptions, parameters, and usage examples for the new surgical tools. `[Chronicler]`

**Verification:** `[The Validator]`
- [ ] Use `grep` to find a pattern in a large file.
- [ ] Use `read_file_range` to read the specific lines found by `grep`.
- [ ] Verify that `read_file` on a massive file returns the suggested tool error instead of attempting to load the whole file.

---

### Phase 3: Pre-emptive Orchestration (Admission Control)
**Objective:** Solve the Compaction Paradox by cleaning context *before* adding new data.

- [ ] **3.1 Implement Size Calculation** `[Architect]`
    - Implement `CalculateProspectiveSize(newMessage)` in `ConversationManager` to estimate the total context size if a message were to be added.
- [ ] **3.2 Refactor Orchestrator Loop** `[Architect]`
    - Change the execution flow in `Orchestrator.cs`:
        - **Current:** `ExecuteTool` $\rightarrow$ `AddMessage` $\rightarrow$ `ShouldCompact`.
        - **New:** `ExecuteTool` $\rightarrow$ `CalculateProspectiveSize` $\rightarrow$ `(If > SoftLimit) CompactAsync` $\rightarrow$ `AddMessage`.
- [ ] **3.3 Admission Validation** `[Architect]`
    - Implement a final check in the `Orchestrator` to ensure the tool result does not exceed the hard limit before committing it to the `ConversationManager`.
- [ ] **3.4 Testing & Documentation**
    - [ ] Create unit tests for `CalculateProspectiveSize` to ensure accuracy. `[QA Engineer]`
    - [ ] Create integration tests for the `Orchestrator` loop to verify that `CompactAsync` is called *before* a message is added when the soft limit is reached. `[QA Engineer]`
    - [ ] Update `docs/architecture.md` to describe the change in the orchestrator loop and the concept of "Admission Control." `[Chronicler]`

**Verification:** `[The Validator]`
- [ ] Trigger a scenario where a tool output is large enough to hit the `SoftLimit`.
- [ ] Verify that `CompactAsync` is called *before* the tool output is added to the history.

---

### Phase 4: Emergency Recovery (The Safety Valve)
**Objective:** Provide a non-LLM dependent recovery path to break deadlock loops.

- [ ] **4.1 Implement Deterministic Pruning** `[Architect]`
    - Implement `EmergencyPruning()` in `ConversationManager`:
        - Keep the System Prompt.
        - Keep the most recent User and AI messages.
        - Discard older messages in FIFO order until total size is $\le$ `EmergencyPruneThreshold`.
- [ ] **4.2 Implement Overflow Interception** `[Architect]`
    - Wrap LLM calls in `Orchestrator` with a try-catch block.
    - On `ContextOverflowException` (or equivalent API error), trigger `EmergencyPruning()`.
    - After pruning, inject a system message: `[SYSTEM]: Emergency context pruning performed due to critical overflow. Some historical context has been lost.`
- [ ] **4.3 Testing & Documentation**
    - [ ] Create unit tests for `EmergencyPruning()` to verify that the correct messages are preserved and discarded. `[QA Engineer]`
    - [ ] Implement a mock LLM failure to trigger the `ContextOverflowException` and verify the recovery flow in the `Orchestrator`. `[QA Engineer]`
    - [ ] Update `docs/context-management.md` to explain the "Emergency Recovery" mechanism and how it breaks the Compaction Paradox. `[Chronicler]`

**Verification:** `[The Validator]`
- [ ] Force a context overflow (e.g., by bypassing limits or using a very small limit).
- [ ] Verify the system automatically prunes the context and continues without crashing.
- [ ] Verify the emergency notification is visible in the history.

---

### Phase 5: System-wide Consistency (Unified Loop)
**Objective:** Ensure subagents inherit all safety guards.

- [ ] **5.1 Abstract Agent Engine** `[Architect]`
    - Identify the common loop logic between Main Agents and Subagents.
    - Extract this logic into a shared `AgentEngine` class/module.
- [ ] **5.2 Integrate Guards into Shared Engine** `[Architect]`
    - Ensure the `AgentEngine` implements:
        - Truncation (Phase 1).
        - Pre-emptive Compaction (Phase 3).
        - Emergency Pruning (Phase 4).
- [ ] **5.3 Subagent Context Filtering** `[Architect]`
    - Apply truncation and safety limits to the "Initial Context" files provided to subagents during dispatch to prevent them from starting in an overflow state.
- [ ] **5.4 Testing & Documentation**
    - [ ] Verify the `AgentEngine` correctly applies all guards across different agent types. `[QA Engineer]`
    - [ ] Test the dispatch of subagents with oversized initial context to verify filtering. `[QA Engineer]`
    - [ ] Update `docs/architecture.md` to document the new `AgentEngine` and how it provides consistent stability across the system. `[Chronicler]`

**Verification:** `[The Validator]`
- [ ] Dispatch a subagent with a massive initial context.
- [ ] Verify the subagent's context is truncated/filtered.
- [ ] Verify the subagent can use surgical tools and recovers from overflows via the shared engine.

---

## 🛠️ Coordination Flow

| Phase | Primary Agent (Architect) | Subagent Task | Sync Point | Verification Gate |
| :--- | :--- | :--- | :--- | :--- |
| **1. Input Guard** | Implement `ContextGuard` logic | **Auditor** maps explosion points | Audit report $\rightarrow$ Guard limits | **Validator** signs off on truncation |
| **2. Admission Control** | Refactor `Orchestrator` loop | **QA Engineer** verifies size calc | Logic verification | **Validator** signs off on pre-emptive compaction |
| **3. Emergency Recovery** | Implement FIFO Safety Valve | **QA Engineer** creates "Context Bombs" | Test failure $\rightarrow$ Fix $\rightarrow$ Pass | **Validator** signs off on recovery |
| **4. Unified Loop** | Abstract `AgentEngine` | **QA Engineer** tests agent consistency | Consistency check | **Validator** signs off on agent consistency |
| **5. Surgical Tools** | Integrate tools into `ToolManager` | **Toolsmith** implements `FileSystem` utils | API availability $\rightarrow$ Integration | **Validator** signs off on surgical tool utility |
| **6. Finalize** | System-wide smoke test | **Chronicler** updates all docs | Documentation sign-off | Final system certification |

## ⚠️ Safety Constraints for Subagents
1. **No Architecture Changes:** Subagents may not modify the `Orchestrator` loop structure without explicit approval from the Architect.
2. **Build Integrity:** Any subagent with `write_file` permissions must run `dotnet build` to ensure no breaking changes were introduced.
3. **Context Economy:** Subagents must summarize their findings; they should not dump entire files back into the main conversation.
