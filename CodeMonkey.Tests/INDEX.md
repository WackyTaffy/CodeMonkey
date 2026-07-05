# CodeMonkey.Tests Index

> The test suites to ensure the stability and correctness of the codebase across Core and UI modules.

## 📐 Interaction Map
**`CodeMonkey.Tests`** ➡️ `CodeMonkey.Core` / `CodeMonkey.UI`

## 🔄 Common Workflows
* **Adding a New Test**:
  1. Create a test class in the corresponding project directory.
  2. Implement test cases.
  3. Run `dotnet test` to verify.

## 📜 Local Rules & Conventions
* Tests must be isolated and avoid side effects on the main system state.
* Prefer unit tests for Core logic and integration tests for Orchestration.
