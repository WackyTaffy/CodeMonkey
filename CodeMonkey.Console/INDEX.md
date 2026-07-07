# CodeMonkey.Console Index

> The CLI entry point for the CodeMonkey system, providing a lightweight interface for agent orchestration.

## 📐 Interaction Map
CLI User ➡️ **`CodeMonkey.Console`** ➡️ `CodeMonkey.Core`

## 🔄 Common Workflows
* **Modifying CLI Behavior**:
  1. Update entry logic in `Program.cs`.
  2. Test via terminal execution.

## 📜 Local Rules & Conventions
* Keep the CLI lean; all business logic should be delegated to `CodeMonkey.Core`.
