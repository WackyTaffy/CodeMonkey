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
The LLM decides it needs to locate the buggy code.

**Message [Assistant]:**
> I will start by searching for the `ExecuteTool` method in `ToolManager.cs` to find the relevant line range.
>
> **Tool Call:** `ReadFileWithSearch`
> **Arguments:** `{ "path": "CodeMonkey.Core/Services/ToolManager.cs", "searchTerm": "ExecuteTool" }`

**Message [Tool]:**
> (Returns a chunk of lines containing the `ExecuteTool` method and surrounding context)

#### 4. Implementation & Verification (Iteration 2)
The LLM identifies the bug and applies a surgical fix.

**Message [Assistant]:**
> I've found the issue. The code is attempting to deserialize the arguments without checking for null or empty strings. I will replace the lines containing the failing logic with a guard clause.
>
> **Tool Call:** `WriteFileRange`
> **Arguments:** `{ "path": "CodeMonkey.Core/Services/ToolManager.cs", "startLine": 42, "endLine": 45, "content": "if (string.IsNullOrEmpty(arguments))\n    {\n        return Result.Failure(\"Arguments cannot be empty\");\n    }", "mode": "Replace" }`

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
> I have fixed the bug in `ExecuteTool` by adding a guard clause to handle empty arguments. I have used a surgical range-based write to minimize token usage. I have verified the fix by running `dotnet build`, which succeeded.
