# Prompt Examples

This document provides a detailed example of the communication flow between CodeMonkey and the LLM.

## Scenario: Fixing a Bug in the ToolManager

**User Request:** "The `ExecuteTool` method is failing when the arguments are empty. Please fix it and ensure the project still builds."

### The Full Conversation Sequence

#### 1. Initialization (The System Prompt & Context)
The application starts by sending the system prompt and the content of the root `INDEX.md`.

**Message [System]:**
> You are an expert .NET developer. You have access to tools to read/write files and run shell commands. Always verify your work by running 'dotnet build'. If you see errors, analyze the output and fix the code. You are working in 'C:\Sourcecode\CodeMonkey'.
>
> Subagent Dispatch:
> Use subagents via 'dispatch_subagent' for repetitive exploration, summarizing large volumes of data, or tasks that would generate excessive tool output. Clearly define the task and grant only necessary permissions (e.g., 'write_file') if the subagent needs to modify the codebase. Subagents return only their final result, keeping your context clean.

**Message [Context]:**
> (Contents of INDEX.md)
> - CodeMonkey.Console: Entry point...
> - CodeMonkey.Core: Business logic...
> - CodeMonkey.Tests: Test suites...

#### 2. The User Request
**Message [User]:**
> The `ExecuteTool` method is failing when the arguments are empty. Please fix it and ensure the project still builds.

#### 3. Agent Reasoning & Tool Call (Iteration 1)
The LLM decides it needs to see the code first.

**Message [Assistant]:**
> I will start by examining the `ToolManager.cs` file to understand how `ExecuteTool` handles arguments.
>
> **Tool Call:** `read_file`
> **Arguments:** `{ "path": "CodeMonkey.Core/Services/ToolManager.cs" }`

**Message [Tool]:**
> (Returns the source code of `ToolManager.cs`)

#### 4. Implementation & Verification (Iteration 2)
The LLM identifies the bug and applies a fix.

**Message [Assistant]:**
> I've found the issue. The code is attempting to deserialize the arguments without checking for null or empty strings. I will add a guard clause.
>
> **Tool Call:** `write_file`
> **Arguments:** `{ "path": "CodeMonkey.Core/Services/ToolManager.cs", "content": "... [Updated Code with null check] ..." }`

**Message [Tool]:**
> Successfully wrote to CodeMonkey.Core/Services/ToolManager.cs

#### 5. Mandatory Verification (Iteration 3)
Following the system prompt, the agent must verify the build.

**Message [Assistant]:**
> Now I will verify that the change didn't break the build.
>
> **Tool Call:** `run_command`
> **Arguments:** `{ "command": "dotnet build" }`

**Message [Tool]:**
> Build succeeded. 0 Error(s).

#### 6. Final Response
**Message [Assistant]:**
> I have fixed the bug in `ExecuteTool` by adding a guard clause to handle empty arguments. I have verified the fix by running `dotnet build`, which succeeded.
