# Pragmatic Roadmap: Stability & DX

## Overview
As a developer, I don't want "visionary" bloat; I want a tool that is reliable, fast, and doesn't break my project. The focus here is on the "Developer Experience" (DX) and robustness.

## Priority Features

### 1. Dry-Run Mode (The "Safety Net")
Implement a `--dry-run` flag. 
- Instead of actually writing files or running commands, the agent outputs a "Proposed Changes" manifest.
- The user must explicitly approve the manifest before execution.

### 2. Integrated Testing Loop
Automate the "Write -> Test -> Fix" cycle.
- Feature: `AutoFix`. If a `dotnet build` or test run fails, the error output is automatically fed back into the LLM to suggest a fix without user intervention.
- This reduces the manual loop of "I told you it's broken, fix it."

### 3. Enhanced Logging & Traceability
Current logs are basic. We need:
- **Execution Trace**: A visual timeline of which tool was called, what the input was, and what the output was.
- **Token Tracking**: Real-time cost/token usage monitoring per request to avoid unexpected API bills.

### 4. Configuration Management
Move from hardcoded values to a `codemonkey.json` or `.yaml` configuration file in the project root.
- Define project-specific rules (e.g., "Always use File-Scoped Namespaces").
- Define excluded directories (e.g., `bin/`, `obj/`, `.git/`) so the agent doesn't waste tokens reading them.
