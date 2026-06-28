# Design Document: Conversation Compaction System

## Overview
The Conversation Compaction System prevents the LLM context window from overflowing by summarizing the conversation history when it reaches a configurable threshold. This ensures the agent maintains a coherent state and operational continuity without exceeding token limits.

## Requirements
- **Token Monitoring**: The system must track the total token count of the current conversation history.
- **Configurable Threshold**: The trigger for compaction should be configurable. (Current target: 15k tokens, with headroom).
- **Compaction Process**:
    - Triggered when the context window token count reaches the configured cap.
    - Create a summary of the conversation history using the LLM.
    - **Exclusions**: The summary process must exclude the system prompt, the most recent round of prompt/response, and tool schemas, as these are re-injected into the fresh context window.
    - **Inclusions**: The summary must identify and include up to the 5 most useful relative file paths involved in the session.
    - **Reset**: After summary generation, the conversation history is reset, and the summary is injected as the initial context.
- **Architecture**:
    - Implement a `ConversationManager` to encapsulate history and compaction logic.
    - Use Inversion of Control (IoC) by injecting `IConversationManager` into the `Orchestrator`.
    - Utilize `GemmaTokenHelper` for accurate token counting.
- **Testing**: Full test coverage using NUnit and NSubstitute.

## Technical Specification

### 1. `IConversationManager` Interface
```csharp
public interface IConversationManager
{
    IEnumerable<Message> GetMessages();
    void AddMessage(Message message);
    bool ShouldCompact(int tokenLimit);
    Task CompactAsync(ILLMClient llmClient, string systemPrompt, IEnumerable<string> toolSchemas);
}
```

### 2. `ConversationManager` Implementation
- **Storage**: Maintains a `List<Message>` of the current session.
- **Token Calculation**: Iterates through messages and uses `GemmaTokenHelper` to sum tokens.
- **Compaction Logic**:
    1. Filters out the system prompt and the last exchange.
    2. Constructs a prompt for the LLM: *"Summarize the following conversation history. Focus on the current objective, key decisions, and state. List up to 5 most important relative file paths. Be concise."*
    3. Calls the LLM to get the summary.
    4. Clears the history and adds the summary as a `system` or `user` message (depending on design) to start the new window.

### 3. `Orchestrator` Integration
The `Orchestrator` will be modified to:
1. Inject `IConversationManager`.
2. Check `ShouldCompact(config.TokenLimit)` before every LLM request.
3. Call `CompactAsync` if compaction is required.
4. Pass the messages from `ConversationManager` to the `LLMClient`.

### 4. Configuration
The token limit will be moved to a configuration object/file to allow easy adjustments without recompilation.

## Implementation Plan
1. **Interface & Model**: Define `IConversationManager`.
2. **Service Implementation**: Implement `ConversationManager` with `GemmaTokenHelper`.
3. **Orchestrator Update**: Refactor `Orchestrator` to delegate history management to `IConversationManager`.
4. **Prompt Engineering**: Define the summary prompt template.
5. **Verification**: Implement unit tests for the compaction trigger and summary logic.
6. **Integration Test**: End-to-end test simulating a large conversation.
