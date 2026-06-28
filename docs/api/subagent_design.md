# Design Document: Subagent Dispatch System

## Overview
The goal is to allow the main agent to offload specific, isolated tasks to subagents. This prevents the main context from being cluttered with long tool-call chains (e.g., exploring a large directory or summarizing multiple files) and allows for specialized context isolation.

## Core Principles
- **No Nesting**: Subagents cannot dispatch other subagents.
- **Context Isolation**: Subagents operate in their own conversation history. The main agent only receives the final result.
- **Least Privilege**: Subagents are restricted from performing destructive actions (like `write_file`) unless explicitly granted permission via a whitelist.
- **Transparency**: Any changes made by a subagent must be reported back to the main agent.
- **Unidirectional Flow**: Subagents cannot call back to the main agent for more information; they must succeed or fail based on the initial context provided.

## Technical Design

### 1. Dispatch Mechanism
A new tool `dispatch_subagent` will be added to the `ToolManager`. 

**Tool Definition:**
- `task`: The specific objective for the subagent.
- `permissions`: A list of allowed "privileged" tools (e.g., `write_file`).
- `initial_context`: A list of files the subagent needs to be aware of.

**Context Injection Logic:**
When the subagent is initialized, the `Orchestrator` will construct the initial prompt by combining:
1. The `task` description.
2. The full content of all files specified in `initial_context`.
3. The subagent-specific system prompt.

### 2. Execution Flow
1. **Main Agent** calls `dispatch_subagent`.
2. **Orchestrator** intercepts this call:
    - Creates a new, empty history for the subagent.
    - Injects the expanded initial context (task + file contents).
    - Sets a flag `IsSubagent = true` to prevent recursive dispatching.
    - Executes a loop similar to the main agent's `ProcessUserRequestAsync` but restricted to the provided permissions.
3. **Subagent** performs the task using the `ToolManager`.
4. **Orchestrator** captures the final response (or the failure/partial work) and returns it as the tool result to the Main Agent.

### 3. Constraints & Security
- **Nesting Prevention**: The `dispatch_subagent` tool will check the `IsSubagent` flag. If true, the tool will return an error: "Subagents cannot dispatch further subagents."
- **Permission Gate**: The `ToolManager` will implement a strict whitelist check. If the current execution context is a subagent, it must have the explicit permission passed during dispatch to execute a tool.
- **Execution Limits**: Subagents are limited to a maximum of 15 iterations. If this limit is reached, the process is terminated and a "timeout/limit reached" status is returned.

### 4. Observability & Error Handling
- **Tracing**: All subagent tool calls and responses are logged to a separate system log (not the main chat history) for debugging and audit purposes.
- **Error Reporting**: If a subagent reaches max iterations or encounters a fatal error, it returns:
    - `status`: "failed" or "partial"
    - `partial_work`: Any data gathered before the failure.
    - `error`: The reason for failure.
- **Success Reporting**: Upon successful completion, the subagent must include a "Changes Made" section in its final response to the main agent.

### 5. System Prompt Guidance
The Main Agent's system prompt will be updated to include:
- **When to dispatch**: "Use subagents for repetitive exploration, summarizing large volumes of data, or tasks that would generate excessive tool output."
- **How to delegate**: "Clearly define the task and grant only the necessary permissions (e.g., `write_file`) if the subagent needs to modify the codebase. Provide a list of files the subagent should start with to minimize unnecessary tool calls."

## Implementation Plan
1. Update `ToolManager` to support permission whitelisting and the `dispatch_subagent` tool.
2. Modify `Orchestrator` to handle the subagent lifecycle:
    - Context expansion (injecting file contents).
    - Execution loop with iteration limits.
    - Result return and logging.
3. Update the main system prompt in `Orchestrator.BootstrapContext`.
4. Add unit tests in `CodeMonkey.Tests` to verify:
    - Subagents cannot dispatch other subagents.
    - Subagents cannot write files without permission.
    - Subagents are terminated after exceeding max iterations.
    - Main agent receives the final summary and a report of changes.
