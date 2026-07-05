# Context Management

This document outlines the strategies and mechanisms implemented to manage the LLM context window and prevent the "Compaction Paradox"—a state where context overflow leads to session crashes or infinite loops during recovery.

## Layered Defense Strategy

The system employs a "Layered Defense" architecture to ensure stability regardless of the size of the data being processed. The defense layers are implemented in the following order of execution:

1. **Input Guard (Phase 1):** Prevents oversized individual payloads from entering the context.
2. **Admission Control (Phase 3):** Performs pre-emptive compaction *before* adding new data.
3. **Emergency Recovery (Phase 4):** A deterministic "Safety Valve" to recover from critical overflows.
4. **Unified Loop (Phase 5):** Ensures these guards are consistent across all agent types (Main and Subagents).
5. **Surgical Tooling (Phase 2):** Provides tools (like `read_file_range` and `grep`) to avoid the need for "Read All" operations.

---

## Input Guard (Phase 1)

The Input Guard is the first line of defense. It intercepts tool outputs and ensures that no single response is large enough to immediately exhaust the context window or trigger an immediate crash.

### Truncation Logic

The `ContextGuard` service is responsible for enforcing limits on incoming strings. When a tool returns a result, the `ContextGuard.Guard()` method is called:

1. **Token Calculation:** The service calculates the token count of the input using the `ITokenHelper`.
2. **Threshold Check:** If the token count is within the specified `maxTokens` limit, the input is returned unchanged.
3. **Truncation:** If the limit is exceeded, the service truncates the content. Since token-perfect slicing is computationally expensive, it uses a character approximation (approximately 4 characters per token).
4. **Notification:** A system notice is appended to the truncated text to inform the agent that data was lost and to suggest using surgical tools for further inspection.

**Truncation Notice:**
> `[SYSTEM NOTICE: This output was too large and has been truncated. To read the remainder, please use 'read_file_range' with specific line numbers or 'grep' to find specific patterns.]`

### Safety Thresholds

The following thresholds are defined in `CodeMonkey.Core.Utility.ContextConstants` to govern the behavior of the guard and the broader context management system:

| Constant | Value | Description |
| :--- | :--- | :--- |
| `MaxToolOutputTokens` | `4000` | The hard limit for a single tool output. Any output exceeding this is truncated by the `ContextGuard`. |
| `SoftLimitTokens` | `10000` | The threshold that triggers pre-emptive compaction. This provides a buffer to accommodate a full `MaxToolOutputTokens` payload without hitting the hard limit. |
| `TotalTokenLimit` | `15000` | The absolute hard limit for the total conversation context. |
| `EmergencyPruneThreshold` | `0.5` | The percentage of context to retain (50%) during a deterministic emergency pruning event. |

## Integration

The Input Guard is integrated into the `Orchestrator` loop, wrapping all tool outputs before they are passed to the `ConversationManager`. This ensures that the LLM never receives a single message that could destabilize the session.
