The following bugs or behaviors have been noted by a user and require investigation.

## File List Tool
- **Observed Behavior:** When the main agent attempts to use the get_file_list tool, it doesn't seem to actually receive the desired output back, leading the agent to do multiple get_file_list attempts followed by using run_command with "dir" as the fallback
	- This may be an issue for subagents as well, but I am unsure
- **Logs:**
	- `C:\Sourcecode\CodeMonkey\.agents\logs\6-28_file-list-tool-fail_1.log`
	- `C:\Sourcecode\CodeMonkey\.agents\logs\6-28_file-list-tool-fail_2.log`

