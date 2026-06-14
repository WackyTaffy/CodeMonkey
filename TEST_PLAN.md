# Test Implementation Plan

This document outlines the strategy and test cases for implementing unit and integration tests for the `CodeMonkey` project.

## Testing Strategy

- **Framework**: xUnit
- **Mocking**: Moq
- **Approach**: 
    - **Unit Tests**: Isolate business logic by mocking all external dependencies via interfaces (`IFileSystem`, `IShell`, `ILLMClient`).
    - **Integration-style Unit Tests**: Use unique temporary directories for `FileSystem` tests to ensure isolation and cleanup.
    - **Network Mocking**: Use `Mock<HttpMessageHandler>` to simulate API responses for `LLMClient` without making actual network calls.

---

## 1. High Testability (Priority: High)

These classes contain the core logic and should have high coverage.

### `Orchestrator`
The `Orchestrator` manages the interaction between the user, the LLM, and the tools.

| Test Case | Scenario | Expected Result |
| :--- | :--- | :--- |
| `BootstrapContext_AddsSystemPrompt` | Call `BootstrapContext` | History should contain the system prompt. |
| `BootstrapContext_AddsIndexMd` | `INDEX.md` exists in working dir | History should contain the contents of `INDEX.md`. |
| `BootstrapContext_SkipsIndexMd` | `INDEX.md` is missing | History should NOT contain `INDEX.md` content. |
| `CompactContext_Summarizes` | Call `CompactContextAsync` | LLM is called with a summary request; history is reset; summary is added. |
| `ProcessRequest_DirectResponse` | LLM returns text content | The method returns the text content immediately. |
| `ProcessRequest_ToolLoop_SingleCall` | LLM returns 1 tool call $\rightarrow$ tool executes $\rightarrow$ LLM returns text | Returns the final text response after one tool execution. |
| `ProcessRequest_ToolLoop_MultiCall` | LLM returns tool A $\rightarrow$ Tool B $\rightarrow$ Text | Returns final text after multiple tool executions. |
| `ProcessRequest_MaxIterations` | LLM returns tools in a loop | The loop terminates after 15 iterations with a "maximum iterations" message. |
| `ProcessRequest_NullResponse` | LLM returns null/empty | The method returns a specific error message. |
| `ProcessRequest_ToolExecutionError` | `ToolManager` throws an exception | The exception is caught, and the error is fed back to the LLM or returned to user. |

### `ToolManager`
`ToolManager` acts as the dispatcher. It must correctly parse JSON and route to the right service.

| Test Case | Scenario | Expected Result |
| :--- | :--- | :--- |
| `ExecuteTool_WriteFile_Success` | name="write_file", valid JSON | Calls `IFileSystem.WriteFile` with exact matching args. |
| `ExecuteTool_ReadFile_Success` | name="read_file", valid JSON | Calls `IFileSystem.ReadFile` with exact matching args. |
| `ExecuteTool_RunCommand_Success` | name="run_command", valid JSON | Calls `IShell.RunCommand` with exact matching args. |
| `ExecuteTool_UnknownTool` | name="invalid_tool" | Returns "Error: Tool invalid_tool not found." |
| `ExecuteTool_InvalidJson` | Malformed JSON arguments | Returns "Error: Invalid arguments" message. |
| `ExecuteTool_ServiceException` | `IFileSystem` throws exception | Returns a formatted error string containing the exception message. |

---

## 2. Medium Testability (Priority: Medium)

These classes interact with the OS or network.

### `LLMClient`
Focuses on the correctness of the request payload and the handling of the API response.

| Test Case | Scenario | Expected Result |
| :--- | :--- | :--- |
| `GetChatCompletion_RequestFormat` | Call `GetChatCompletionAsync` | The JSON request body contains the correct model, messages, and tool definitions. |
| `GetChatCompletion_Success` | API returns valid 200 OK JSON | Returns a correctly populated `ChatResponse` object. |
| `GetChatCompletion_ApiError` | API returns 400, 401, or 500 | Throws an `HttpRequestException` or returns a custom error response. |
| `GetToolDefinitions_Structure` | Call `GetToolDefinitions` | Returns 3 tools with correct JSON schemas for `write_file`, `read_file`, and `run_command`. |

### `FileSystem`
Ensures physical file operations behave as expected.

| Test Case | Scenario | Expected Result |
| :--- | :--- | :--- |
| `ReadFile_ExistingFile` | File exists in unique temp dir | Returns the exact content of the file. |
| `ReadFile_MissingFile` | File does not exist | Returns "File not found." |
| `WriteFile_Success` | Write content to unique temp dir | File is created on disk with the specified content. |
| `FileExists_True` | File exists | Returns `true`. |
| `FileExists_False` | File does not exist | Returns `false`. |

---

## 3. Integration Tests (Priority: Low)

These tests verify that components work together in a real-world flow.

- **End-to-End Tool Flow**: 
    - Setup: Mock `LLMClient` to return a `read_file` call $\rightarrow$ Mock `FileSystem` to return specific content $\rightarrow$ Mock `LLMClient` to return a final response based on that content.
    - Verification: `Orchestrator.ProcessRequest` returns the expected final answer.
- **Context Persistence**: 
    - Verify that `Orchestrator` maintains history across multiple `ProcessRequest` calls.

---

## Implementation Roadmap

1. **Infrastructure Setup**: 
    - Create `CodeMonkey.Tests` project.
    - Add references to `CodeMonkey.Core` and `CodeMonkey.Console`.
    - Install `xunit`, `moq`, `Microsoft.NET.Test.Sdk`, and `xunit.runner.visualstudio`.
2. **Phase 1 (Logic)**: 
    - Implement `ToolManager` tests.
    - Implement `Orchestrator` tests (using mocks for `ILLMClient`, `IToolManager`).
3. **Phase 2 (IO/Network)**: 
    - Implement `FileSystem` tests (using `Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())`).
    - Implement `LLMClient` tests (using `Mock<HttpMessageHandler>`).
4. **Phase 3 (Integration)**: 
    - Implement high-level flow tests.
5. **Verification**: 
    - Execute `dotnet test`.
    - Measure coverage (optional).
