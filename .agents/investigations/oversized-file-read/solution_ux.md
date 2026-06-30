# UX Solution: Handling Oversized File Reads

## Overview
To prevent the agent from crashing due to context overflow when reading large files, we must move from a "blind read" approach to a "guarded read" approach. This solution focuses on how the system communicates the limitation to the agent and the user, and what alternative mechanisms are provided to ensure the agent can still complete its task.

## 1. Communication Strategy

### A. Tool-Level Feedback (The "Guard")
Instead of allowing a massive file to enter the context window and trigger a crash, the `read_file` tool should perform a pre-read size check. If the file exceeds a safety threshold (e.g., 8,000 tokens), the tool should not return the file content.

**Proposed Tool Error Message:**
> `Error: The file 'path/to/file' is too large to be read in its entirety (Size: X tokens). To analyze this file, please use 'read_file_range' to read specific line segments or 'grep' to search for specific patterns.`

### B. Agent-to-User Communication
When the agent receives the error above, it should communicate the situation to the user transparently and propose a plan.

**Example Agent Response:**
> "I attempted to read `LargeFile.cs`, but it's too large for me to process all at once. I will instead search for the relevant sections using `grep` and read specific blocks of code to find the answer to your request."

## 2. Proposed Alternative Tools

To ensure the agent is not "blinded" by large files, we need to provide "surgical" read capabilities.

### A. `read_file_range`
Instead of reading the whole file, the agent should be able to request specific segments.
- **Inputs:** `path`, `startLine`, `endLine`.
- **UX Benefit:** Allows the agent to iterate through a large file in chunks without overflowing the context.

### B. `grep` / `search_in_file`
The agent should be able to find specific keywords or regex patterns.
- **Inputs:** `path`, `pattern`.
- **Output:** Line numbers and a small snippet of the matching line.
- **UX Benefit:** Allows the agent to identify exactly which line ranges are worth reading via `read_file_range`.

### C. `read_file_head` / `read_file_tail`
Quickly inspect the beginning or end of a file.
- **Inputs:** `path`, `lineCount`.
- **UX Benefit:** Useful for checking file headers, imports, or the end of a class definition.

## 3. Revised User Journey

1. **User Request:** "Explain how the logic in `GiantService.cs` works."
2. **Agent Action:** Calls `read_file('GiantService.cs')`.
3. **System Guard:** `FileSystem` detects file is > 8k tokens $\rightarrow$ Returns "Error: File too large... use `read_file_range` or `grep`."
4. **Agent Communication:** Tells user: "The file is quite large, so I'll search for the core logic first and then read the relevant sections."
5. **Agent Action (Surgical):** 
   - Calls `grep('GiantService.cs', 'ProcessOrder')` $\rightarrow$ Gets line 450.
   - Calls `read_file_range('GiantService.cs', 440, 500)`.
6. **Final Result:** Agent provides the explanation based on the targeted snippets.

## 4. Summary of Changes

| Component | Current Behavior | Proposed Behavior |
| :--- | :--- | :--- |
| **`read_file`** | Returns full content regardless of size | Validates size $\rightarrow$ returns content OR "Too Large" error |
| **Tooling** | All-or-nothing read | Range-based reads and pattern searching |
| **Communication** | System crashes (Silent failure/Loop) | Explicit error $\rightarrow$ Agent explains strategy $\rightarrow$ Surgical read |
