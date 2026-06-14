# YAML Migration Phased Implementation Plan

## Objective
Migrate the `CodeMonkey` project from JSON to YAML for LLM communication and internal tool argument passing to improve readability and consistency.

## Implementation Principles
- **Idempotency**: Each phase must be designed such that applying it multiple times results in the same state without introducing errors or duplicates.
- **Compatibility**: Ensure that the system remains functional at the end of each phase.

---

# Phases
**Last Completed Phase**: 3

## Phase 1: Model Preparation
**Goal**: Prepare data models for YAML serialization without removing JSON support.

1. **Analyze Models**: Review `ChatResponse`, `Choice`, `FunctionCall`, `Message`, and `ToolCall`.
2. **Add YAML Mapping**: If necessary, add `YamlMember` attributes from `YamlDotNet` to properties to ensure consistent naming, mirroring existing `JsonPropertyName` attributes.
3. **Idempotency Check**: If `YamlMember` attributes already exist on a property, do not add them again.

## Phase 2: Outgoing Request Migration
**Goal**: Ensure all requests sent to the LLM are serialized as YAML.

1. **Update `LLMClient.SendRequest`**: Verify that the outgoing payload is serialized using `YamlDotNet`.
2. **Verify Request Format**: Ensure the HTTP headers (e.g., `Content-Type`) are updated to `application/x-yaml` or the appropriate YAML mime-type.
3. **Idempotency Check**: If the `LLMClient` is already utilizing the `YamlSerializer` for the request body, skip this step.

## Phase 3: Incoming Response Migration
**Goal**: Transition the processing of LLM responses from JSON to YAML.

1. **Update `LLMClient.DeserializeResponse`**: Change the deserialization logic to use `YamlDotNet.Deserializer` instead of `System.Text.Json.JsonSerializer`.
2. **Fallback Logic**: (Optional) Implement a check to handle both JSON and YAML during the transition period to prevent breaking changes.
3. **Idempotency Check**: If the response handling logic already uses the YAML deserializer, skip this step.

## Phase 4: Internal Tooling Migration
**Goal**: Migrate tool argument passing from JSON to YAML.

1. **Update `ToolManager.ExecuteTool`**: Change the `argsJson` parameter (and its usage) to `argsYaml`.
2. **Update Tool Callers**: Update the `Orchestrator` or any service calling `ExecuteTool` to pass YAML-serialized arguments.
3. **Idempotency Check**: If `ToolManager` is already processing YAML strings, skip this step.

## Phase 5: Final Cleanup & Verification
**Goal**: Remove legacy JSON dependencies and verify system stability.

1. **Remove JSON Attributes**: Remove `[JsonPropertyName]` and `[JsonIgnore]` attributes from models.
2. **Purge JSON Imports**: Remove `using System.Text.Json` and `using System.Text.Json.Serialization` from all files.
3. **Verify via Tests**: Run the full test suite in `CodeMonkey.Tests` to ensure no regressions.
4. **Idempotency Check**: Only remove imports and attributes if they are no longer referenced in the codebase.
