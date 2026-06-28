# CodeMonkey

CodeMonkey is a .NET-based agentic framework designed to bridge the gap between high-level architectural intent and executable code. It provides a robust environment for autonomous agents to reason about, modify, and verify codebases with precision.

## 🛠️ Tech Stack
- **Runtime:** .NET 8.0
- **Language:** C#
- **Orchestration:** `<LLM_FRAMEWORK>` (e.g., Semantic Kernel / LangChain)
- **Verification:** xUnit / dotnet test

## 🚀 Project Vision

The goal of CodeMonkey is to create a "Developer's Co-Pilot" that doesn't just suggest code, but understands architectural intent, maintains consistency across large codebases, and ensures stability through rigorous automated verification.

### Core Principles
- **Architectural Integrity:** Preventing system erosion by maintaining a clear mapping of the system's structure (tracked via `CONTEXT-MAP.md`).
- **Agentic Autonomy:** Implementing a ReAct-style reasoning loop that allows AI agents to plan, execute .NET tools, and self-correct based on build output.
- **Verification-Driven Development:** Ensuring every autonomous change is backed by tests and build verification.
- **Pragmatic Evolution:** Evolving the system based on real-world usage, documented through Architectural Decision Records (ADRs) in `docs/adr/`.

## 📂 Project Structure

```text
/
├── docs/                  # Detailed specifications and ADRs
│   └── adr/               # Architectural Decision Records
├── src/                   # Source code
│   └── <PROJECT_NAME>/    # Main application logic and entry point
├── tests/                 # Test suites for verification
├── AGENTS.md              # "Laws of the Land" for AI Agents
├── CONTEXT-MAP.md         # High-level structural mapping
└── README.md              # Project entry point
```

**Main Entry Point:** `src/<PROJECT_NAME>/Program.cs`

## 🏁 Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A compatible IDE (Visual Studio 2022, JetBrains Rider, or VS Code)
- API Keys for the required LLM provider (e.g., OpenAI, Anthropic)

### Installation & First Run
Follow these steps to get CodeMonkey running locally:

1. **Clone the Repository**
   ```bash
   git clone <REPOSITORY_URL>
   cd CodeMonkey
   ```

2. **Configure Environment**
   Create a `.env` file (or `appsettings.json`) in the root directory based on the template:
   ```bash
   cp .env.example .env
   ```
   Open `.env` and add your keys:
   ```text
   LLM_API_KEY=your_api_key_here
   ```

3. **Restore & Build**
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Run the Application**
   ```bash
   dotnet run --project src/<PROJECT_NAME>
   ```

**Success Criteria:** Upon a successful first run, you should see the `Welcome to CodeMonkey` prompt in your terminal, indicating the agentic loop is initialized and ready for input.

## ⚙️ Configuration
CodeMonkey relies on environment variables for sensitive credentials.
- **`LLM_API_KEY`**: The primary key for the orchestration layer.
- **`AGENT_MODE`**: (Optional) Set to `debug` or `production` to control the verbosity of the reasoning loop.

## 🤖 AI Agent Onboarding
If you are an AI agent operating on this repository, please follow this sequence to gain full context:
1. **Read [AGENTS.md](./AGENTS.md):** This contains the coding standards and "Laws of the Land."
2. **Review [CONTEXT-MAP.md](./CONTEXT-MAP.md):** Use this to map namespaces to files and understand the system boundaries.
3. **Analyze [docs/adr/](./docs/adr/):** Review these files to understand the *why* behind core architectural decisions before proposing changes.

## 🤝 Contributing
We welcome contributions that enhance the agentic capabilities or stability of the framework.
1. Ensure all changes are documented in a new ADR if they affect architecture.
2. All PRs must pass `dotnet build` and `dotnet test`.
3. Update `CONTEXT-MAP.md` if new high-level modules are introduced.

## 📜 License
This project is licensed under the `<LICENSE_TYPE>` License - see the LICENSE file for details.

---
*Welcome to CodeMonkey. Let's build something stable.*
