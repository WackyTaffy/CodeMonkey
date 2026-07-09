# Tool Reference Guide

This document provides a comprehensive reference for the tools available to agents within the CodeMonkey framework. These tools are exposed via the `ToolManager` and allow agents to interact with the file system and the host environment.

## 🛠️ Tool Categories

### 📂 File System - Read Operations
These tools allow agents to explore and read the codebase.

| Tool | Description | Arguments | Output |
| :--- | :--- | :--- | :--- |
| `read_file` | Reads the entire content of a file. | `Path` (string) | Full file content. |
| `read_file_chunked` | Reads a specific range of lines from a file. | `Path` (string), `StartLine` (int), `EndLine` (int) | Lines within the specified range. |
| `read_file_search` | Searches for a term and returns matching lines with context. | `Path` (string), `SearchTerm` (string), `ContextLines` (int) | Matching lines + surrounding context. |
| `read_file_head` | Reads the first N lines of a file. | `Path` (string), `LineCount` (int) | First N lines of the file. |
| `read_file_tail` | Reads the last N lines of a file. | `Path` (string), `LineCount` (int) | Last N lines of the file. |
| `grep` | Searches for a regex pattern in a file. | `Pattern` (string), `Path` (string) | All lines matching the pattern. |
| `file_exists` | Checks if a specific file exists on disk. | `Path` (string) | `"True"` or `"False"`. |
| `get_file_list` | Lists files in a directory, optionally recursive. | `Recursive` (bool), `SearchPattern` (string) | List of relative file paths. |

### ✍️ File System - Write Operations
These tools allow agents to modify the codebase. **These are privileged tools.**

| Tool | Description | Arguments | Effect |
| :--- | :--- | :--- | :--- |
| `write_file` | Overwrites a file with new content or creates it. | `Path` (string), `Content` (string) | File is replaced with provided content. |
| `write_file_range` | Performs a surgical update to a specific line range. | `Path` (string), `StartLine` (int), `EndLine` (int), `Content` (string), `Mode` (FileWriteMode) | Modifies only the specified range of the file. |

### 💻 System Operations
These tools allow agents to execute external commands. **These are privileged tools.**

| Tool | Description | Arguments | Output |
| :--- | :--- | :--- | :--- |
| `run_command` | Executes a shell command in the working directory. | `Command` (string) | Standard output/error of the command. |

---

## 🛡️ Permissions & Security

Some tools are marked as **Privileged**. When a subagent is dispatched, the `ToolManager` checks if the agent has been granted explicit permission to use these tools.

**Privileged Tools List:**
- `write_file`
- `write_file_range`
- `run_command`

If a subagent attempts to use a privileged tool without permission, the `ToolManager` will return an error indicating the lack of authorization.

## 📝 Usage Notes

- **Paths**: All paths should be relative to the project root unless otherwise specified.
- **Token Limits**: Tool output is subject to token limits. Large outputs may be truncated to prevent context overflow.
- **Surgical Updates**: For large files, prefer `write_file_range` or `read_file_chunked` over `write_file` and `read_file` to conserve tokens and reduce risk.
