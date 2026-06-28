# Design Specification: Code Monkey UI (Blazor Hybrid)

## 1. High-Level Architecture
The system will be implemented as a **Blazor Hybrid (MAUI)** application. This allows the UI to run as a native desktop application with full access to the local filesystem while utilizing web technologies for the interface.

### 1.1 Integration Model
- **Direct Reference**: The Blazor UI project will reference `CodeMonkey.Core` directly.
- **Process Model**: The UI and the `Orchestrator` will run in the same OS process.
- **Lifecycle**: The UI will instantiate and manage the lifecycle of the `Orchestrator` service.

## 2. Communication & Data Flow
### 2.1 Real-time Streaming
To achieve a responsive "AI feel," the system will avoid traditional request-response patterns for logs.
- **Mechanism**: `IAsyncEnumerable<T>`.
- **Flow**: The `Orchestrator` (or a wrapper service) will expose methods that return `IAsyncEnumerable<LogEvent>`.
- **UI Consumption**: The Blazor components will use `await foreach` to update the UI in real-time as events are emitted.

### 2.2 Control Flow
- **Cancellation**: Every request to the Orchestrator will pass a `CancellationToken`. The UI will provide a "Stop" button that triggers the cancellation, allowing the agent to perform a "Soft Cancel."
- **User Interrupts**: The agent can push "Prompt" events into the stream. The UI will render these as interactive cards, but the user's input field remains active for "Free Input" at all times.

## 3. Storage Strategy (Dual-Storage)
### 3.1 Application State (Relational)
- **Technology**: SQLite.
- **Purpose**: Persistent storage of conversation history, session metadata, and user configurations.
- **Implementation**: Entity Framework Core (EF Core) for structured access.

### 3.2 Codebase Memory (Vector)
- **Technology**: `Microsoft.SemanticKernel.Memory`.
- **Purpose**: Semantic search across the local codebase (RAG).
- **Implementation**: 
    - **Initial phase**: Local file-based vector store.
    - **Indexing**: Automatic background scan of the project directory on application startup.

## 4. UI/UX Design
### 4.1 Visual Identity
- **Theme**: "IDE Dark" (High contrast, dark background, Monospace fonts for code).
- **Layout**:
    - **Left Sidebar**: Interactive File Tree (Context Management).
    - **Center Pane**: Chat Interface (Conversation & Streaming Logs).
    - **Right Pane**: Side-by-Side File Viewer (Read-only).

### 4.2 Key Components
- **Chat Window**: 
    - Streaming text for AI responses.
    - Interactive logs: File paths in logs are clickable.
    - Inline Prompt Cards: For agent questions/approvals.
- **File Tree**:
    - Allows users to explicitly include/exclude files from the agent's current context.
    - Roadmap: Evolution into a Full File Explorer.
- **File Viewer**:
    - Displays file content when a log link is clicked or a file is selected in the tree.

## 5. Technical Roadmap
1.  **Phase 1: Infrastructure**: Set up MAUI project, integrate `CodeMonkey.Core`, and implement the `IAsyncEnumerable` streaming bridge.
2.  **Phase 2: Core UI**: Implement the "IDE Dark" layout, Chat Window, and Basic File Tree.
3.  **Phase 3: Storage**: Implement SQLite session persistence and Semantic Kernel codebase indexing.
4.  **Phase 4: Advanced Interaction**: Implement Side-by-Side viewer, Interactive Logs, and Prompt Cards.
5.  **Phase 5: Refinement**: Polish UX, optimize indexing speed, and finalize the "Full File Explorer" transition.
