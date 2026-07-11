## Streaming
I want the reasoning and response generation streamed to the UI (only if not using Console window as primary UI) so I can see the response being created in real time

## Stop generation
I want to be able to stop the current agent(s) and give a completely new prompt. Sometimes I can tell that the agent is going down the wrong path and I want a way to stop and redirect it without killing the entire session and losing conversation history

## Workflow Scripts
I want to be able to have the LLM generate a deterministic workflow script like Claude Code CLI's workflow scripts. An implementation plan would be something like a JSON file or JS script so that the C# code can handle all orchestration and context management without needing to rely on the non-deterministic agent reasoning to adhere to the plan.

## Codebase Quality Agents
A set of agents that run periodically (initial implementation: every X minutes) to ensure codebase is up to a set of defined standards. Each of these quality agents will be multi phased: one phase to identify gaps/ambiguities/issues, and another phase to fix those issues

#### Agents
| Name                         | Goal                                                                                                                                                       | Initial Context                                |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------- |
| In-line Docs                 | All code has appropriate in-line documentation                                                                                                             | Target Code File                               |
| Human Docs                   | Documentation exists to allow a human to onboard to the code and fully understand it.                                                                      | Target doc file, list of all documentation     |
| Agent Docs                   | Documentation exists to allow an AI-agent to reason about the repo and understand conventions/standards (navigation docs are handled by a different agent) | Target doc file, list of all documentation     |
| Test Coverage                | Code has full test coverage; including edge cases, failure modes, semantic correctness, etc                                                                | Target Code File, Target Test File             |
| File Size                    | All files must be under 2000 tokens                                                                                                                        | Target file                                    |
| Navigation Docs              | Docs (i.e. INDEX.md) exist such that an AI-agent could navigate the repo efficiently; All files are captured by exactly one index file                     | Full file list                                 |
| Code Standards & Conventions | Ensure all code adheres to standards and conventions                                                                                                       | Standards & conventions docs, target code file |
| Directory Structure          | Ensure the directory structure & organization is up to best-practices and standards                                                                        | Standards & conventions docs, full file list   |
|                              |                                                                                                                                                            |                                                |

## Smart Context Management
Context can be split into the following categories, each of which should have it's own context management strategy.

The conversation history will be stored in a some sort of flat file, like a `.log` or `.json`. As the context grows, the oldest conversation items (user prompts, ai responses, tool output, etc) will be removed from the active context that is being given to the LLM on each turn. If the agent wants to search/read the conversation history it will need to use surgical tools because the history file will be too large to read in total.

#### Context Categories

| Type                | Requires Temporal Context | Context String Order    | Compaction | Notes                                                                                                                                                                                                                                                                                  |
| ------------------- | ------------------------- | ----------------------- | ---------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| System Prompt       | No                        | 0                       | Never      | System instructions + Tool defintions                                                                                                                                                                                                                                                  |
| User Prompts        | Yes                       | N/A - See Convo History |            | Compacted as part of Convo History                                                                                                                                                                                                                                                     |
| Agent Responses     | Yes                       | N/A - See Convo History |            | Compacted as part of Convo History                                                                                                                                                                                                                                                     |
| Agent Plans         | No                        | 1                       | Never      | An agent should be limited to one plan at a time. Once the plan is approved by a human, the agent executes on each step/component of the plan. Any modifications to the plan must be human-approved. The plan allows the agent to retain it's objective when conversation is compacted |
| Tool Calls + Output | Yes                       | N/A - See Convo History |            | Compacted as part of Convo History. Output of tool must be tied to the original tool call.                                                                                                                                                                                             |
| File Contents       | No                        | 2                       | On-demand  | A set amount of tokens reserved for file contents. Remove files that have not been read in X number of turns. Update context's version of file instead of multiple versions of the file throughout the conversation history                                                            |
| Convo History       |                           | 3                       | Proactive  | Collection of temporally contextuallized User Prompts, Agent Responses, and Tool Call/Output.                                                                                                                                                                                          |


## Dynamic System Prompt
The system prompt is dynamically built and populated based on an initial exploration of the repository. 
- Build and test instructions - If there is an exact project or solution file, it should attempt to build with that
- Language and versioning

## Color Coded Console Logs
The console logs need to be color coded so I can differentiate the type of log, which agent is it being logged from, etc

## Additional Tools

| Name                    | Description                                                                                                                       |
| ----------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| set_implementation_plan | Takes in a string that should contain an implementation plan for the current system goal. This plan is added to the context as a  |
