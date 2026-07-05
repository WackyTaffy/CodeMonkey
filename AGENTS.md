# Agent Handbook (`AGENTS.md`)

This document provides essential guidelines, standards, and navigation instructions for AI agents operating within the CodeMonkey repository. Following these rules ensures consistency, maintainability, and reduces the risk of architectural drift.

## 🛡️ The Laws of the Land (Anti-Patterns)

To maintain a clean and stable codebase, avoid the following:

1.  **No Silent Failures:** Never wrap large blocks of code in empty `try-catch` blocks. All exceptions must be logged or handled explicitly.
2.  **No Magic Strings/Numbers:** Avoid hardcoded configuration values. Use constants or configuration files.
3.  **Avoid Over-Engineering:** Do not implement generic patterns (e.g., excessive abstraction) unless the project requirements explicitly demand them. Prioritize readability and pragmatism over theoretical perfection.
4.  **No Breaking Changes without Documentation:** Do not modify public API signatures or core data structures without updating the relevant `INDEX.md` and creating a new ADR in `docs/adr/`.
5.  **Do Not Bypass Tests:** Never disable tests or mark them as `[Ignore]` to pass a build. Fix the underlying issue or update the test to reflect the new expected behavior.

## 💎 Standard of Quality

### Coding Standards
- **Naming:** Follow standard .NET naming conventions (PascalCase for classes/methods, camelCase for private fields with `_` prefix).
- **Async/Await:** Use `Async` suffix for all asynchronous methods. Always use `CancellationToken` in async methods where applicable.
- **Error Handling:** Use a centralized error handling strategy. Prefer returning Result patterns or throwing domain-specific exceptions over generic `Exception` types.
- **Documentation:** Every public-facing method must have a brief XML comment explaining its purpose and parameters.

### PR Requirements for Agents
When proposing changes:
- **Contextual Alignment:** Explain how the change fits into the architecture defined in `CONTEXT-MAP.md`.
- **Verification:** Provide evidence of `dotnet build` and `dotnet test` success.
- **Doc Updates:** If the change modifies architectural boundaries, propose a corresponding update to the documentation.

## 🗺️ Repository Navigation

To efficiently reason about this project, follow this sequence:

1.  **`INDEX.md` (START HERE):** This is your GPS. Use the Tiered Indexing system (Root $\rightarrow$ Project $\rightarrow$ Leaf) to navigate the codebase without loading unnecessary files.
2.  **`CONTEXT-MAP.md`:** Use this to understand the conceptual architecture and high-level dependencies ("The Blueprint").
3.  **`docs/adr/`:** Consult the Architectural Decision Records to understand *why* certain patterns were chosen before proposing "optimizations."

---
*This document is a living entity. If you identify a recurring anti-pattern or a gap in the standards, propose an update to this file.*
