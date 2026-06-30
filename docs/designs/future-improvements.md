## Line Ranges for File CRUD
- READ
	- Instead of reading an entire file at once, expose a tool for the LLM to request a read in a range of lines in a file
	- If the agent doesn't know the exact line range, allow it to perform a search in the file to retrieve a set of lines around each search match. If there are multiple search matches and their line ranges overlap, combine those into a single line range.
	- EXAMPLES
		- Read line range (i.e. "I need to read the first 5 lines of the file") -> returns those 5 lines only
		- Read out-of-range line numbers (i.e. "read line 45-60 in file-with-50-lines.txt") -> returns only lines 45-50 that actually exist
		- Read lines around mentions of `compaction` (i.e. "find where compaction is does in file1.cs") -> finds search matches on lines 5, 7, 13, and 34 -> returns lines 3-9, 11-15, and 32-36
- WRITE
	- Instead of writing an entire file to modify a single line in the file, expose a tools for the LLM to request a modification of a specific range of lines
	- EXAMPLES
		- Modification of existing lines (i.e. "replace line 23 with this text...") -> same number of lines in file
		- Insertion of new lines (i.e. "after line 20 add this text...") -> increase number of lines in file
		- Deletion of lines (i.e. "delete lines 20-30") -> decrease number of lines in file
- DOCUMENTATION
	- Allow line ranges to be specifically called out in documentation to allow targeted/focused operations

## Display Reasoning
- I want to see what the reasoning is behind each of the LLM responses. I know a string with the reasoning is usually returned with the response. For now the UI doesn't need show the reasoning streaming, I just want to be able to view the reasoning behind each tool call

## Streaming
- I want the reasoning and response generation streamed to the UI (only if not using Console window as primary UI) so I can see the response being created in real time

## Stop generation
- I want to be able to stop the current agent(s) and give a completely new prompt. Sometimes I can tell that the agent is going down the wrong path and I want a way to stop and redirect it without killing the entire session and losing conversation history

## Workflow Scripts
- I want to be able to have the LLM generate a deterministic workflow script like Claude Code CLI's workflow scripts