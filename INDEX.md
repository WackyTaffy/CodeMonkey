# Codebase Index

> CodeMonkey is an AI-powered developer agent system designed for high-context codebase manipulation and orchestration.

## 🧭 Navigation Protocol
**STOP:** To explore any module or directory, you MUST read the `INDEX.md` file within that directory before listing files or reading source code. This ensures maximum context efficiency and prevents token overflow.

## 🛠️ Project Structure & Entry Points
* **`CodeMonkey.Console/`**: CLI entry point $\rightarrow$ `Program.cs`
* **`CodeMonkey.Core/`**: Business logic & LLM orchestration engine $\rightarrow$ `INDEX.md`
* **`CodeMonkey.UI/`**: .NET MAUI / Blazor hybrid UI $\rightarrow$ `Main.razor`
* **`CodeMonkey.UI.Rendering/`**: Specialized markdown rendering engine $\rightarrow$ `INDEX.md`
* **`CodeMonkey.Tests/`**: Test suites for core and UI logic $\rightarrow$ `INDEX.md`
* **`docs/`**: Comprehensive project documentation $\rightarrow$ `INDEX.md`

## 📐 Architecture & Dependencies
`CodeMonkey.Console` $\rightarrow$ `CodeMonkey.Core` $\rightarrow$ `LLM APIs`
`CodeMonkey.UI` $\rightarrow$ `CodeMonkey.UI.Rendering` $\rightarrow$ `CodeMonkey.Core` $\rightarrow$ `LLM APIs`

## 🗺️ Critical Paths
* **Orchestration**: `CodeMonkey.Core/Services/Orchestrator.cs` ➡️ LLM Communication ➡️ Tool Execution.
* **UI Flow**: `CodeMonkey.UI/Main.razor` ➡️ `CodeMonkey.UI/ViewModels/ChatViewModel.cs` ➡️ `CodeMonkey.Core`.
* **Context Mgmt**: `CodeMonkey.Core/Services/Orchestrator.cs` (Bootstrap) ➡️ `INDEX.md` files.
* **Risk Mgmt**: `CodeMonkey.Core/Services/ManifestService.cs` ➡️ Action Approval Flow.

## 🚀 Developer Quick-Start
* **Build**: `dotnet build`
* **Test**: `dotnet test`

## 📜 Complementary Guides
- **Behavior & Standards**: See [AGENTS.md](./AGENTS.md) for the "Laws of the Land."
- **Architectural Blueprint**: See [CONTEXT-MAP.md](./CONTEXT-MAP.md) for high-level system design.

## ⚙️ Global Rules
1. **Context Optimization**: Maintain `INDEX.md` files in every significant directory.
2. **Interface-First**: All core services must be defined by interfaces.
3. **Documentation**: Architectural changes must be reflected in `docs/designs/`.
