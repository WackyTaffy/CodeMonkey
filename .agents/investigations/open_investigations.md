The following bugs or behaviors have been noted by a user and require investigation.

## Oversized File Read Context Overflow
- **Observed Behavior:** The agent could read a file that is greater in size than the agent's context window (i.e. 15k context window cannot hold a 20k token file). Once an oversized file is added to the context, it causes all further LLM requests to fail because of the context overflow. This cannot even be self healed due to the inability to compact the context once it is over that size. 
	- Oversized files can also cause compaction-thrashing where the agent reads file -> hit's compaction trigger due to context size -> compacts (which loses the file content) -> must read file again to get the contents -> context overflow -> compaction -> cycle continues
- **Logs:**
	- `C:\Sourcecode\CodeMonkey\.agents\logs\6-28_file-list-tool-fail_2.log`
	- Behavior seen in subagent -> `C:\Sourcecode\CodeMonkey\.agents\logs\6-29_overloading-subagents.log`
	- Behavior caused by "dir" command -> `C:\Sourcecode\CodeMonkey\.agents\logs\6-29_dir-overload.log`
- **Investigation Subdirectory:** `C:\Sourcecode\CodeMonkey\.agents\investigations\oversized-file-read`

## Self-Destructing Command Call
- **Observed Behavior:** The agent is able to kill it's own process with `taskkill` using the `run_command` tool.
- **Desired Behavior:** If the app is about to do something that could destroy it's own process, tell it that is what will happen and advise that it should escalate to a human
- **Logs:**
	- `C:\Sourcecode\CodeMonkey\.agents\logs\6-28_self-kill.log`

## Confusion during output
- **Logs:**
	- `C:\Sourcecode\CodeMonkey\.agents\logs\6-28_response-confusion.log`

## Loses User Prompt on compaction?
