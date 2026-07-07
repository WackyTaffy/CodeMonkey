# CodeMonkey.Core Index

> The core engine containing the business logic, LLM orchestration, and system services.

## 📐 Interaction Map
`CodeMonkey.UI` / `CodeMonkey.Console` ➡️ **`CodeMonkey.Core`** ➡️ `LLM APIs`

## 🔄 Common Workflows
* **Adding a New Capability**:
  1. Define Interface in `Interfaces/`
  2. Implement concrete logic in `Services/`
  3. Register the service in `Services/Orchestrator.cs` (if applicable).

* **Updating Domain Models**:
  1. Modify DTOs in `Models/`
  2. Update dependent services in `Services/`.

## 📂 Directory Mappings
* **`Interfaces/`**: Public contracts for system services.
* **`Models/`**: Core domain models and DTOs.
* **`Services/`**: Implementation of the system's business logic.
* **`Utility/`**: Shared helpers and constants.

## 📜 Local Rules & Conventions
* Strict adherence to Dependency Inversion; services must depend on interfaces.
* Core logic must remain agnostic of the UI implementation.
