# CodeMonkey.Core/Services Index

> Implementation of core business logic and system services.

## 📐 Interaction Map
**`Services`** ➡️ `Interfaces` (Implements) / `Models` (Uses) / `Utility` (Uses)

## ☢️ Blast Radius
- **HIGH**: This is the engine room. Changes here can fundamentally alter system behavior and stability.

## 🚀 Primary Entry Points
- **`Orchestrator.cs`**: The main agentic loop and coordination logic.
- **`LLMClient.cs`**: Handles the actual API communication with LLMs.
- **`ToolManager.cs`**: Manages the discovery and execution of system tools.

## 📂 Service Groups
- **Execution Engine**: `Orchestrator.cs`, `ProcessRunner.cs`, `Shell.cs`
- **AI Integration**: `LLMClient.cs`, `ContextGuard.cs`
- **Data & State**: `ConversationManager.cs`, `SessionLedger.cs`, `UserPreferences.cs`, `ManifestService.cs`
- **System Access**: `FileSystem.cs`, `SecureFileSystem.cs`, `GitService.cs`, `LogManager.cs`

## 📜 Local Rules & Conventions
- Services should depend on interfaces, not concrete implementations.
- Business logic must be decoupled from UI concerns.
