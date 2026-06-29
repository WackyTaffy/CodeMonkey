# Handoff: Code Monkey UI Implementation (Blazor)

## Objective
The goal is to design and implement a professional-grade user interface for the Code Monkey application using **Blazor Hybrid (MAUI)**. The UI serves as a "Developer Command Center," integrating agent control, project state, and native filesystem access.

## Core Requirements

### 1. Interaction & Developer Experience (DX)
- **Keyboard-First Workflow**:
    - Global hotkeys for common actions (Send, Stop, Save).
    - A **Command Palette** (`Ctrl+K`) for quick navigation and agent commands.
- **IDE & VCS Integration**:
    - **Git Status**: Real-time display of the current active Git branch in the UI status bar.
    - **Jump to File**: Clickable file paths in logs and context that open the file in the user's configured IDE.
    - **Side-by-Side Diff**: Visual comparison of current vs. proposed file changes with granular Accept/Reject controls.
- **Context Control**:
    - Ability to add files/folders via glob patterns.
    - Token pressure indicator (visual gauge of context window usage).

### 2. Intent-Based Execution (HITL 2.0)
Instead of simple approval modals, the system uses a **Manifest + Confidence** model:
- **The Manifest**: For complex operations, the agent proposes an "Execution Plan" (a manifest of intended changes and commands) before execution.
- **Confidence-Based Gating**:
    - **Low Risk** (e.g., `read_file`, `list_dir`): Auto-approved; executed silently.
    - **Medium Risk** (e.g., modifying existing code in known files): Auto-approved but flagged in the log with a quick "Revert" option.
    - **High Risk** (e.g., `run_command`, `delete_file`, large-scale refactors): Execution is blocked until the user approves the Manifest.

### 3. Secure Rendering Pipeline
To eliminate XSS risks and increase UI control, the system avoids raw HTML rendering:
- **AST-to-Component Rendering**: 
    - The LLM's Markdown output is parsed into an **Abstract Syntax Tree (AST)** via `Markdig`.
    - The AST is mapped directly to **Native Blazor Components** (e.g., `MarkdownCodeBlock`, `MarkdownTable`).
    - **Zero `MarkupString`**: By using standard Blazor data binding, XSS is mathematically impossible as no raw HTML strings from the LLM are ever rendered directly.

### 4. Performance & Reliability
- **Log Management**:
    - Throttled rendering (batching) to prevent UI thread exhaustion.
    - Bounded circular buffers for logs to prevent memory leaks.
- **State Integrity**:
    - Atomic session saves (write-to-temp then rename).
    - Global exception handling to isolate Orchestrator failures from the UI process.
- **Responsiveness**:
    - Response streaming for chat bubbles via `IAsyncEnumerable`.

## Integration Architecture
- **Hosting**: Blazor Hybrid (MAUI).
- **Decoupling**: UI $\rightarrow$ `IOrchestrator` / `ViewModel` abstraction.
- **Core Services**:
    - `GitService`: For branch tracking and VCS state.
    - `ManifestService`: For managing proposed agent plans.
    - `ComponentRenderer`: For the AST $\rightarrow$ Blazor Component pipeline.
