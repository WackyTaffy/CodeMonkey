# CodeMonkey.Core/Interfaces Index

> Defines the contracts for core system services and components.

## 📐 Interaction Map
**`Interfaces`** ⬅️ `Services` / `ViewModels` / `UI.Rendering`

## ☢️ Blast Radius
- **HIGH**: Modifications to interfaces typically require updates across multiple service implementations and calling sites.

## 🚀 Primary Entry Points
- **`IOrchestrator.cs`**: The central coordination contract.
- **`ILLMClient.cs`**: The contract for LLM communication.

## 📂 Implementation Details
- **System Core**: `IOrchestrator.cs`, `IProcessRunner.cs`, `IShell.cs`
- **AI/LLM**: `ILLMClient.cs`, `ITokenHelper.cs`
- **State/Persistence**: `IConversationManager.cs`, `ISessionLedger.cs`, `IUserPreferences.cs`
- **Resources**: `IFileSystem.cs`, `IManifestService.cs`, `IToolManager.cs`

## 📜 Local Rules & Conventions
- All interfaces must follow the `I[Name]` naming convention.
- Interfaces should remain lean, focusing only on public contracts.
