# CodeMonkey.Core/Models Index

> Data transfer objects and domain models used across the core.

## 📐 Interaction Map
**`Models`** ➡️ `Interfaces` / `Services` / `UI.Rendering`

## ☢️ Blast Radius
- **MEDIUM**: Changing a model affects data serialization and all services that consume these objects.

## 🚀 Primary Entry Points
- **`Manifest.cs`**: Defines the codebase structural representation.
- **`Message.cs`**: The fundamental unit of communication.

## 📂 Model Categories
- **LLM Communication**: `ChatResponse.cs`, `Choice.cs`, `FunctionCall.cs`
- **System State**: `Manifest.cs`, `Message.cs`
- **Tooling/Execution**: `ToolArgs.cs`, `ToolCall.cs`, `ToolResult.cs`, `SubagentDispatchArgs.cs`

## 📜 Local Rules & Conventions
- Models should be primarily POCOs (Plain Old CLR Objects).
- Use immutable properties where possible to ensure thread safety.
