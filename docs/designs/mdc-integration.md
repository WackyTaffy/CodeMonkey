## Engineering Handoff: Modular Markdown Component (`.mdc`) Integration

This document maps out the implementation strategy for extending the `CodeMonkey` agent harness with a dynamic prompt-injection pipeline. This pipeline shields our local 15k token Gemma context window from token exhaustion by pulling rules on demand.

---

## 1. Architectural Definition: What is an `.mdc` File?

A Markdown Component (`.mdc`) is a machine-readable configuration and instruction block. It contains standard Markdown text topped with a strict YAML frontmatter block.

Unlike standard `.md` design documents, `.mdc` files are explicitly designed for your runtime engine (`ContextGuard`) to parse. The harness evaluates the metadata at every turn of the conversation to inject localized rules into the system prompt _only_ when the agent interacts with matching files.

## Implementation Blueprint: `.agents/skills/maui-blazor-ui.mdc`

```markdown
---
description: Constraints for UI view components, layout styling, and Blazor Hybrid data-binding.
globs: "CodeMonkey.UI/**/*.razor, CodeMonkey.UI/**/*.xaml.cs, CodeMonkey.UI.Rendering/**/*.cs"
alwaysApply: false
---

# MAUI Blazor UI Architectural Standards

- **Thread Management**: Always dispatch long-running tasks through `IOrchestrator`. Never block the main UI rendering thread.
- **Component Isolation**: Keep Razor components state-isolated. Pass structural UI events upward using `EventCallback`.
- **Style Boundaries**: Place visual styles strictly inside scoped `.razor.css` files. Do not bloat global XAML ResourceDictionaries.
```


### How MDC Metadata Affects Agent Workflow
#### 1. Activating the "Gated Context"
In a large repository, loading every single architectural rule file into the LLM's prompt window at the same time would crush your context window and cause token bloat.

- The Filter: The harness reads the `globs` metadata field (`"CodeMonkey.UI/**/*.razor..."`).
- The Trigger: When the AI agent uses its file-system tools to read, edit, or create a file that matches one of those three specified path patterns, the harness instantly intercepts the request.
- The Injection: The harness automatically attaches the contents of this `.mdc` file into the prompt context for that specific turn. If the agent is editing `backend/server.js`, this file is completely hidden from it.

#### 2. Combating "LLM Amnesia"

If `alwaysApply` were set to `true`, the harness would inject this UI rulebook into _every single prompt_, even when the agent is writing database migrations. This leads to information overload, and the model might forget its core instructions.

By utilizing the targeted `globs` list, the harness guarantees the agent is hyper-focused on Blazor thread management and component isolation only when it is physically touched or looking inside the `CodeMonkey.UI` directory.

#### 3. Guiding the Agent's Self-Correction

When an agent harness evaluates a file path change against this metadata, it acts as a passive linter.

If the agent uses a `glob` tool to find all `.razor` files, the harness matches those paths against this `.mdc` frontmatter and silently attaches a system note: _"You are working with files covered by MAUI Blazor UI Architectural Standards. Review these constraints."_ The agent then knows it cannot block the main thread and must use scoped `.razor.css` files before it even begins typing code.


---

## 2. Taxonomy: When to use `.md` vs `.mdc`

To prevent rule pollution and maintain clean repository boundaries, use this explicit configuration matrix:

```text
Is this document meant to be evaluated as runtime behavior constraints by the agent?
 ├── YES ── Is it bound to a specific project layer, file pattern, or specialized workflow?
 │           ├── YES ──> Create an `.mdc` file inside `.agents/skills/`
 │           └── NO  ──> Keep in root System Prompt or AGENTS.md (Global Bounds)
 └── NO  ── Is it historical reference, design rationale, or deep architecture maps for human review?
             └── YES ──> Create an `.md` file inside the `docs/` hierarchy
```

|Architectural Attribute|Standard `.md` File|Markdown Component (`.mdc`)|
|---|---|---|
|Primary Consumer|Humans (and Agents only via explicit tool reads)|`ContextGuard` pipeline (Auto-injected into prompt)|
|Context Token Footprint|0 tokens ( dormant until explicitly read )|Dynamic (0 to 1,500 tokens based on active globs)|
|Parsing Requirement|Standard Markdown text parser|YAML Frontmatter Parser + Markdown Reader|
|Primary Placement|Root directory or inside `docs/`|Exclusively under `.agents/skills/`|
|Best Used For|Design specs, onboarding guides, ZIP release notes|Subsystem constraints, testing loops, tool guardrails|

---

## 3. The Onion-Skin Model: Progressive Disclosure Architecture

With a 15k token ceiling on your local `llama-server`, the agent must never perform deep recursive scans. It must traverse the repository using an intentional, layered approach:

```text
[ Layer 1: Root Skeleton ]       --> Read INDEX.md / CONTEXT-MAP.md to locate a domain.
            │
[ Layer 2: Sub-Project Anchor ] --> Read CodeMonkey.Core/INDEX.md to find explicit files.
            │
[ Layer 3: Dynamic Leaf Node ]   --> Tool triggers file read; Harness injects matching .mdc rules.
```

## Layer 1: Root System Maps (`INDEX.md`, `CONTEXT-MAP.md`)

- Target Size: Under 300 tokens combined.
- Content Constraint: Pure high-level maps. Single-sentence definitions of the main solution assemblies (`CodeMonkey.Core`, `CodeMonkey.UI`). Zero class names or code snippets.
- Operational Rule: Read once during initial session discovery to find where a specific domain lives.

## Layer 2: Sub-Project Indexes (`CodeMonkey.Core/INDEX.md`)

- Target Size: Under 250 tokens per file.
- Content Constraint: Architectural summaries of that specific project namespace (e.g., mapping `Interfaces/` to `Services/`).
- Operational Rule: Eliminates the need for the agent to run raw directory listing commands, keeping the token context clean.

## Layer 3: Dynamic `.mdc` Leaves (Inside `.agents/skills/`)

- Target Size: Under 500 tokens per component.
- Content Constraint: Highly specific engineering guardrails targeting a precise codebase domain.
- Operational Rule: Completely hidden until the agent targets a matching glob, ensuring pinpoint relevance.

---

## 4. Engineering Spec: C# Context Window Pipeline

To implement this inside `CodeMonkey.Core/Services/ContextGuard.cs`, use the following logic to evaluate file arrays, run glob matching, and safely compile the system prompt for your local model.

## C# Contract Definition

```csharp
namespace CodeMonkey.Core.Interfaces;

public interface IContextGuard
{
    string CompileSystemPrompt(
        string baseSystemPrompt, 
        IEnumerable<string> activeFileContexts, 
        string chatHistory);
}
```

## Core Architecture Implementation Engine

```csharp
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using CodeMonkey.Core.Utility; // Contains GemmaTokenHelper

namespace CodeMonkey.Core.Services;

public class ContextGuard : Interfaces.IContextGuard
{
    private readonly string _skillsDirectory = @"C:\Sourcecode\CodeMonkey\.agents\skills";
    private readonly int _maxTokenBudget = 15000;

    public string CompileSystemPrompt(string baseSystemPrompt, IEnumerable<string> activeFileContexts, string chatHistory)
    {
        var activeRules = new StringBuilder();
        var processedComponents = new HashSet<string>();

        if (Directory.Exists(_skillsDirectory))
        {
            var mdcFiles = Directory.GetFiles(_skillsDirectory, "*.mdc");
            foreach (var mdcFile in mdcFiles)
            {
                var (frontmatter, markdownContent) = ParseMdcFile(mdcFile);

                // Condition A: Component is globally active
                if (frontmatter.AlwaysApply)
                {
                    activeRules.AppendLine(markdownContent);
                    continue;
                }

                // Condition B: Component matches file boundaries actively touched by the session
                foreach (var filePath in activeFileContexts)
                {
                    if (PathGuard.MatchesGlob(filePath, frontmatter.Globs) && !processedComponents.Contains(mdcFile))
                    {
                        activeRules.AppendLine(markdownContent);
                        processedComponents.Add(mdcFile);
                        break;
                    }
                }
            }
        }

        // Combine the foundational prompt with the dynamic contextual extensions
        var dynamicPrompt = $"{baseSystemPrompt}\n\n### ACTIVE EXTENDED CONSTRAINTS:\n{activeRules}";

        // Context Protection: Ensure we don't overflow the 15k limit
        int estimatedTokens = GemmaTokenHelper.CountTokens(dynamicPrompt) + GemmaTokenHelper.CountTokens(chatHistory);
        if (estimatedTokens > _maxTokenBudget)
        {
            return ResolveTokenOverflow(baseSystemPrompt, chatHistory);
        }

        return dynamicPrompt;
    }

    private (MdcFrontmatter Frontmatter, string Content) ParseMdcFile(string path) => throw new NotImplementedException();
    private string ResolveTokenOverflow(string basePrompt, string history) => throw new NotImplementedException();
}

public class MdcFrontmatter
{
    public string Description { get; set; } = string.Empty;
    public string Globs { get; set; } = string.Empty;
    public bool AlwaysApply { get; set; }
}
```

---

## 5. Critical Pitfalls & Local 30B Failure Modes

When executing modular prompts against a local Gemma model, keep an eye out for these structural failure points:

## Pitfall 1: Overlapping Glob Injections (Context Contamination)

- The Failure Model: Registering wide patterns like `globs: "CodeMonkey.Core/**/*.cs"` across multiple `.mdc` files. The engine will accidentally pull in Git, FileSystem, and LLM rules simultaneously during a routine modify step.
- The Fix: Enforce narrow directory boundaries on your globs (e.g., `globs: "CodeMonkey.Core/Services/Git*.cs"`). Add a safety assertion inside `CodeMonkey.Tests/ConfidenceGatingTests.cs` that fails a test build if any single file in the repo satisfies more than two `.mdc` components simultaneously.

## Pitfall 2: Directives in Conflict (Instruction Paralysis)

- The Failure Model: An older rule file commands: _"Enforce synchronous execution execution patterns inside command pathways,"_ while a new rule file states: _"All IProcessRunner calls must operate asynchronously."_ Gemma will either hallucinate interfaces, throw erratic exceptions, or hang during text generation.
- The Fix: Ensure rules are strictly decoupled. If a standard changes project-wide, elevate it to `AGENTS.md` or a root architectural `.mdc` file instead of splitting contradictory variations into sub-folders.

## Pitfall 3: Subagent Rule Blindness (Context Dropping)

- The Failure Model: The primary orchestrator evaluates file modifications correctly and injects the proper rules into its own prompt. It then dispatches a subagent via `dispatch_subagent` but only sends a raw command string. The child subagent generates code that completely violates your system boundaries.
- The Fix: Your `SubagentDispatchArgs` model must be updated to track and pass an active list of compiled rules down into the child context window, giving your subagents the exact same guardrails.
