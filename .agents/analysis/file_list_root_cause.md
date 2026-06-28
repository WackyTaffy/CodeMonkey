# Root Cause Analysis: File List Tool Failure

## Observed Behavior
The `get_file_list` tool frequently returns `"Error: Invalid arguments"`, causing the agent to retry several times or fallback to using `run_command` with `dir`.

## Root Cause
The issue lies in the `ToolManager.ParseArguments` method in `CodeMonkey.Core/Services/ToolManager.cs`.

### Technical Details
The `ToolManager` attempts to deserialize the tool arguments JSON into a `Dictionary<string, string>`:

```csharp
public Dictionary<string, string>? ParseArguments(string argsJson)
{
    try
    {
        return JsonSerializer.Deserialize<Dictionary<string, string>>(argsJson, _options);
    }
    catch
    {
        return null;
    }
}
```

When the LLM calls `get_file_list`, it typically provides a boolean value for the `recursive` parameter:
`{"recursive": true}`

In JSON, `true` is a boolean literal, not a string. `JsonSerializer.Deserialize<Dictionary<string, string>>` fails when it encounters a non-string value in the JSON object, causing the method to return `null`. Consequently, `ExecuteTool` returns the "Error: Invalid arguments" message.

## Proposed Solutions

### 1. Use `Dictionary<string, object>` or `JsonElement`
Modify `ParseArguments` to handle `object` or `JsonElement` instead of `string` values. This allows the deserializer to handle booleans, numbers, and strings.

### 2. Implement Tool-Specific Argument Models
Instead of a generic dictionary, define a class/struct for each tool's arguments (e.g., `GetFileListArgs`). This provides type safety and allows `System.Text.Json` to handle types correctly.

### 3. Use `JsonDocument` for Dynamic Parsing
Use `JsonDocument.Parse(argsJson)` to manually extract values, converting them to strings as needed before passing them to the service layer.

## Recommendation
The most robust solution is **Option 2 (Tool-Specific Models)** as the project grows, but **Option 1 (Dictionary<string, object>)** is the quickest fix to restore functionality across all tools.
