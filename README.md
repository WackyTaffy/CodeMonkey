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
- **Architectural Integrity:** Preventing system erosion by maintaining a clear mapping of the system's structure.
- **Agentic Autonomy:** Implementing a ReAct-style reasoning loop that allows AI agents to plan, execute .NET tools, and self-correct based on build output.
- **Verification-Driven Development:** Ensuring every autonomous change is backed by tests and build verification.
- **Pragmatic Evolution:** Evolving the system based on real-world usage, documented through Architectural Decision Records (ADRs).

## 📂 High-Level Project Structure

The repository is organized into functional modules:
- `CodeMonkey.Console`: CLI Entry point.
- `CodeMonkey.Core`: Orchestration and business logic.
- `CodeMonkey.UI`: User interface and rendering.
- `CodeMonkey.Tests`: Quality assurance.
- `docs/`: Architectural designs and ADRs.

**Main Entry Point:** `CodeMonkey.Console/Program.cs`

## 🏁 Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A compatible IDE (Visual Studio 2022, JetBrains Rider, or VS Code)
- API Keys for the required LLM provider (e.g., OpenAI, Anthropic)

### Installation & First Run
1. **Clone the Repository**
   ```bash
   git clone <REPOSITORY_URL>
   cd CodeMonkey
   ```

2. **Configure Environment**
   Create a `.env` file based on the template and add your keys:
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
   dotnet run --project CodeMonkey.Console
   ```

## ⚙️ Configuration
CodeMonkey relies on environment variables for sensitive credentials.
- **`LLM_API_KEY`**: The primary key for the orchestration layer.
- **`AGENT_MODE`**: (Optional) Set to `debug` or `production`.

## 🤖 AI Agent Onboarding
If you are an AI agent operating on this repository, you must initialize your context in this order:
1. **[INDEX.md](./INDEX.md):** Your primary navigation tool (GPS). Start here to locate files.
2. **[AGENTS.md](./AGENTS.md):** Your rulebook. Read this to understand coding standards and the "Laws of the Land."
3. **[CONTEXT-MAP.md](./CONTEXT-MAP.md):** The architectural blueprint. Use this to understand high-level dependencies and system intent.

## 🤝 Contributing
1. Ensure all changes are documented in a new ADR if they affect architecture.
2. All PRs must pass `dotnet build` and `dotnet test`.
3. Update the corresponding `INDEX.md` files if new modules are introduced.

## 📜 License
This project is licensed under the `<LICENSE_TYPE>` License.
