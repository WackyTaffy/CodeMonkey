# Visionary Architecture: CodeMonkey 2.0

## Overview
The goal is to evolve CodeMonkey from a linear tool-executor into a sophisticated, self-evolving AI ecosystem.

## Architectural Proposals

### 1. Plugin-Based Tooling System
Instead of hardcoding tools in the `ToolManager`, implement a dynamic plugin architecture. 
- **Discovery**: Discovery of tools via reflection or a plugin directory.
- **Extensibility**: Allow third-party developers to write `.dll` plugins that implement a `ICodeMonkeyTool` interface.
- **Hot-Reloading**: Load and unload tools without restarting the application.

### 2. Multi-Agent Swarm Orchestration
Move away from a single `Orchestrator` to a "Council of Agents".
- **The Lead Architect**: Breaks down the high-level goal into a plan.
- **Librarian**: Manages project context and retrieves relevant code snippets from a vector store.
- **The Implementer**: Writes the code.
Librarian should be used by the Implementer and the Lead Architect.
- **The Quality Guard**: Runs tests and and reviews the the same.

### 3. Contextual Memory via Vector Database
Replace the basic chat history with a hybrid memory system.
- **Short-term**: Current session window.
- **Long-term**: A local vector database (e.g., ChromaDB or FAISS) that contains embeddings of the entire codebase. This allows the agent to "remember" patterns and logic across different modules without filling the context window.

### 4. Event-Driven Execution Engine
Implement an internal event bus. 
- When a tool is executed, it publishes a `ToolExecutedEvent`.
- Other agents (like the Quality Guard) can subscribe to these events to trigger validation steps automatically.
