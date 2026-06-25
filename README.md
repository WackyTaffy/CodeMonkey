# CodeMonkey

CodeMonkey is an AI-powered .NET development assistant that can read, write, and execute code within a provided workspace. It leverages a loop-based agent architecture.

Currently only a single agent context can be used, i.e. no subagents yet.

## Getting Started

### 1. LLM Backend (llama.cpp)

CodeMonkey is designed to work with a `llama.cpp` server. 

#### Developer Environment
Used llama.cpp to host [Gemma 4 31B **Instruct** Q4-K-M](https://huggingface.co/google/gemma-4-31B-it)
Uses [Google's Gemma 4 Prompt Formatting](https://ai.google.dev/gemma/docs/core/prompt-formatting-gemma4) for request payload

**Recommended Server Command:**
```bash
./llama-server --model "file-path-to-gguf" --host 0.0.0.0 --port 8080 --gpu-layers -1 --ctx-size 32768 --flash-attn on --parallel 2 --threads 12 --threads-batch 8 --cache-ram 4096
```

**Developer Hardware Specs:**
The configuration above was optimized for a machine with:
- **GPU:** 24GB VRAM
- **RAM:** 64GB
- **CPU:** Intel i9 275HX

### 2. Running CodeMonkey

You can run the application in several ways:
- **IDE Debugging:** Open the solution in **Visual Studio** or **VSCode** and run the `CodeMonkey.Console` project.
- **Manual Installation:** Follow the detailed instructions in [docs/installation-guide.md](docs/installation-guide.md).

## Prompt Architecture

CodeMonkey uses a specific prompt structure to guide the LLM in acting as a professional developer.

### Main Agent
The main agent is initialized with a system prompt that defines its persona and capabilities:

> You are an expert .NET developer. You have access to tools to read/write files and run shell commands. Always verify your work by running 'dotnet build'. If you see errors, analyze the output and fix the code. You are working in '{workingDirectory}'.

Additionally, the contents of the `INDEX.md` file in the root of the working directory are provided as context to give the agent an immediate overview of the project.

For a detailed walkthrough of a full agentic loop, see the [Prompt Examples](docs/prompt-examples.md).
## Project Structure

- `CodeMonkey.Console`: The entry point and CLI for the application.
- `CodeMonkey.Core`: Core business logic, LLM client, and tool management.
- `CodeMonkey.Tests`: Test suite for ensuring stability.
- `docs/`: Documentation and design specifications.

## Development Journey
- Initial, bare-bones implementation coded exclusively through LM studio chats
- All further iterations have been done by CodeMonkey itself with direction from me (on a similar level to basic Claude Code CLI use)
- Minimal human interaction, mostly stylistic changes here and there while reviewing the code