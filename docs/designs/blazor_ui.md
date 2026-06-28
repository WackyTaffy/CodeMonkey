# Handoff: Code Monkey UI Implementation (Blazor)

## Objective
The goal is to design and implement a user interface for the Code Monkey application. The user has expressed interest in using **Blazor**.

## Current State
- The application currently exists as a Console application (`CodeMonkey.Console`).
- The core logic, including the LLM orchestration, tool management, and conversation handling, is encapsulated in `CodeMonkey.Core`.
- The `Orchestrator` handles the main agent loop and sub-agent dispatch.
- The system is highly modular, making it suitable for integration with a new UI layer.

## Requirements for the Next Agent
The next agent must **not** start coding immediately. Instead, they are required to use the `grill-me` skill to stress-test and refine the design with the user.

### Specific Tasks:
1. **Design Phase (Mandatory)**: Use the `.agents/skills/grill-me/SKILL.md` process to interview the user about the Blazor UI design. 
   - Focus on: Hosting model (WASM vs Server), State management, Interaction patterns (streaming logs vs static updates), and Integration points with `CodeMonkey.Core`.
   - Ensure a shared understanding is reached and a design specification is documented before any implementation begins.
2. **Architecture Planning**: Define how the Blazor app will communicate with the `Orchestrator` (e.g., via a Web API, a shared service, or a direct reference in a hybrid app).
3. **Implementation**: Once the design is locked, implement the UI following the agreed-upon specification.

## Reference Files
- `CodeMonkey.Core/Services/Orchestrator.cs`: Main logic for agent execution.
- `.agents/skills/grill-me/SKILL.md`: The required process for design refinement.
- `INDEX.md`: Project structure overview.

## Success Criteria
- A detailed design specification for the Blazor UI.
- A functional UI that allows users to interact with Code Monkey, view logs in real-time, and manage their project context.
