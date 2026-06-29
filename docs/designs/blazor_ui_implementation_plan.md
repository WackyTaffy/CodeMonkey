# Execution Instructions for AI Agent

You are tasked with implementing the Code Monkey Blazor UI. This document is the **Single Source of Truth** for the implementation.

### Operating Rules:
1. **Sequential Execution**: Proceed through the phases in order (Phase 1 $\rightarrow$ Phase 6). Do not start a new phase until all tasks in the current phase are completed and verified.
2. **State Tracking**: This file is a living document. You **must** update the checkboxes `[ ]` to `[x]` immediately upon the successful completion of each task.
3. **Definition of Done (DoD)**: A task is only "Done" when:
    - The code is implemented and builds successfully.
    - The specific tests defined in the **Verification & Testing** section of that phase are written and passing.
    - The implementation follows the architectural decisions in Section 1.
4. **Subagent Strategy**: For tasks explicitly marked as **"Subagent Task"**, you should dispatch a subagent to handle the boilerplate or isolated logic to keep your main context clean.
5. **Testing Mandate**: Use **NUnit** and **NSubstitute**. Do not skip tests. If a test fails, the task is not complete.
6. **Persistence**: Save all changes to the codebase and this tracking file before concluding a session.

---

# Implementation Plan: Code Monkey Blazor UI (Hardened & Idempotent)

## 1. Architectural Decision Matrix
Professional-grade stability, security, and maintainability for the Developer Command Center.

| Decision | Selected Option | Reasoning |
| :--- | :--- | :--- |
| **Hosting Model** | **Blazor Hybrid (MAUI)** | Native filesystem access; avoids API server complexity. |
| **Communication** | **Abstraction Layer (`IOrchestrator`)** | Decouples UI from Core; enables testing and potential Client-Server migration. |
| **Log Streaming** | **Dual-Stream (Buffer + File)** | Circular buffer for UI performance; persistent file log for auditing. |
| **Persistence** | **Atomic Save (Temp $\rightarrow$ Rename)** | Prevents session corruption during crashes. |
| **Cancellation** | **CancellationToken** | Standard .NET approach for graceful tool interruption. |
| **Rendering** | **AST-to-Component Pipeline** | Maps Markdown AST $\rightarrow$ Blazor Components. Zero `MarkupString`. |
| **UI Scaling** | **Component Virtualization** | Uses `<Virtualize>` for logs and file lists to handle large projects. |

## 2. Integration Strategy
- **Service Layer**: `CodeMonkey.UI` project.
- **Dependency Injection**:
    - `IOrchestrator`: Singleton core logic.
    - `IMarkdownComponentRenderer`: Singleton AST pipeline.
    - `IGitService`: VCS tracking with graceful degradation.
    - `IManifestService`: Proposed execution plans.
    - `ISessionLedger`: Structured audit trail of all changes.
    - `IUserPreferences`: Stores Trust Profiles (Strict, Balanced, Trusting).
- **State Management**: `UIViewModel` mediating between `IOrchestrator` and `.razor` components.

## 3. Hardened Security & Intent Model
- **Path Sanitization**: All paths passed to the filesystem must be normalized via `Path.GetFullPath()` and validated against `ProjectRoot` to prevent directory traversal.
- **Manifest + Confidence Gating**:
    - **Low Risk**: Auto-approved.
    - **Medium Risk**: Auto-approved (per Trust Profile), recorded in `SessionLedger` with "Undo".
    - **High Risk**: Blocked until approved via Manifest Reviewer or Command Palette.
- **Trust Profiles**:
    - `Strict`: Manual approval for Medium/High risk.
    - `Balanced`: Auto-approve Medium, Manual for High.
    - `Trusting`: Auto-approve all except destructive (Delete/Shell) commands.

## 4. DX & Productivity Features
- **IDE Integration**: `vscode://` protocol links with URI validation.
- **VCS Visibility**: Status bar showing current branch; fails silently if `.git` is missing.
- **Visual Diff**: `DiffPlex` side-by-side view with granular hunk acceptance.
- **Command Palette (`Ctrl+K`)**: 
    - Quick commands and navigation.
    - Shortcut for "Approve Pending Manifest".
- **Keyboard-First Flow**: `Enter` (Send), `Shift+Enter` (NewLine), `Esc` (Stop), `Ctrl+S` (Save).

## 5. Implementation Phases
*Note: All steps are defined as state-requirements. Re-running a step verifies that the state is still correct and applies fixes if it has drifted.*

### Phase 1: Infrastructure & Base Services
- [x] **1.1 Project Initialization**: Ensure the `CodeMonkey.UI` MAUI Blazor project exists and is correctly configured to reference `CodeMonkey.Core`. 
    - **Subagent Task**: Scaffold the project structure and initial project file references.
- [x] **1.2 DI Container Configuration**: Ensure the DI container is configured with `IOrchestrator`, `IUserPreferences`, and `ISessionLedger` using singleton lifecycles.
- [x] **1.3 Git Integration**: Ensure `IGitService` is implemented and correctly detects the active branch with a fail-safe fallback.
    - **Subagent Task**: Implement the `IGitService` logic using `LibGit2Sharp` or CLI wrappers.
- [x] **1.4 Logging Pipeline**: Ensure `LogManager` is implemented to provide both a `ConcurrentQueue` for the UI and a persistent file stream for disk.
- **Verification & Testing**: 
    - Implement NUnit tests for `IGitService` and `LogManager`.
    - Use NSubstitute to mock filesystem access and Git repositories.

### Phase 2: Intent & Security Framework
- [x] **2.1 Filesystem Guard**: Ensure all path-based operations are normalized via `Path.GetFullPath()` and validated against the `ProjectRoot`.
    - **Subagent Task**: Implement the `PathGuard` utility and comprehensive validation logic.
- [x] **2.2 Manifest Definitions**: Ensure `Manifest` data structures and the `IManifestService` are implemented to handle proposed agent actions.
- [x] **2.3 Confidence Gating Logic**: Ensure the tool-execution pipeline correctly routes requests based on the active `Trust Profile`.
- [x] **2.4 Audit Ledger**: Ensure `ISessionLedger` is implemented and consistently records every executed manifest.
    - **Subagent Task**: Implement the `ISessionLedger` persistence logic.
- **Verification & Testing**: 
    - Extensive NUnit test suite for `PathGuard` (testing directory traversal attacks).
    - Use NSubstitute to verify that `IManifestService` triggers the correct gating logic based on `TrustProfile`.

### Phase 3: Rendering Engine
- [x] **3.1 AST Rendering Pipeline**: Ensure the `Markdig` $\rightarrow$ AST $\rightarrow$ Blazor Component mapping logic is implemented and tested.
    - **Subagent Task**: Implement the AST visitor and mapping logic.
- [x] **3.2 Markdown Components**: Ensure `MarkdownCodeBlock`, `MarkdownTable`, and `MarkdownLink` (with IDE integration) are implemented as native Blazor components.
    - **Subagent Task**: Scaffold the individual `.razor` components for each Markdown element.
- [x] **3.3 Virtualized Log View**: Ensure the terminal log view is implemented using `<Virtualize>` to ensure O(1) rendering performance regardless of log size.
- **Verification & Testing**: 
    - Unit tests for the AST mapping logic (Input Markdown $\rightarrow$ Expected Component Type).
    - Component tests to ensure `MarkdownLink` correctly formats `vscode://` URIs.

### Phase 4: Core Chat & Interaction
- [ ] **4.1 Streaming Interface**: Ensure the chat UI implements `IAsyncEnumerable` token streaming for real-time response rendering.
- [ ] **4.2 Input Handler**: Ensure the chat input component supports the defined keyboard shortcuts and correctly triggers the `CancellationToken` for the "Stop" signal.
- [ ] **4.3 Command Palette**: Ensure the `Ctrl+K` overlay is implemented and correctly resolves both global commands and pending manifests.
- **Verification & Testing**: 
    - Use NSubstitute to mock `IOrchestrator` streaming responses to test UI reactivity.
    - Verify `CancellationToken` propagation from the UI to the core service.

### Phase 5: Productivity Suite
- [ ] **5.1 Manifest Reviewer UI**: Ensure the UI for reviewing, editing, and approving high-risk manifests is implemented.
- [ ] **5.2 Side-by-Side Diff**: Ensure the `DiffPlex` component is integrated to provide a visual comparison and granular hunk acceptance.
    - **Subagent Task**: Create the `DiffView` wrapper component around `DiffPlex`.
- [ ] **5.3 Status Dashboard**: Ensure the status bar is implemented to display the Git branch, Token gauge, and Project Root.
- **Verification & Testing**: 
    - NUnit tests for diff hunk calculation and acceptance logic.
    - Integration tests for the Manifest Review $\rightarrow$ Approval $\rightarrow$ Execution flow.

### Phase 6: Hardening & Final Polish
- [ ] **6.1 Atomic Persistence**: Ensure all session saving logic utilizes the Temp $\rightarrow$ Rename pattern to prevent data corruption.
    - **Subagent Task**: Implement the atomic file write utility.
- [ ] **6.2 UI Error Boundaries**: Ensure a global `ErrorBoundary` is implemented to isolate core failures from the UI process.
- [ ] **6.3 Resource Validation**: Ensure that memory profiling confirms the absence of leaks in the `<Virtualize>` components and circular buffers.
- [ ] **6.4 UX Accessibility**: Ensure the UI meets ARIA standards and is fully navigable via keyboard only.
- **Verification & Testing**: 
    - Stress tests for Atomic Persistence (simulating crashes during write).
    - End-to-end smoke tests for the complete "Query $\rightarrow$ Manifest $\rightarrow$ Execute $\rightarrow$ Verify" loop.
