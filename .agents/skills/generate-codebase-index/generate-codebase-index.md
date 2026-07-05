---
id: generate-codebase-index
name: Codebase Index File Generation
description: Analyzes repository architecture to create a hyper-compact index file for AI context optimization.
version: 1.2.0
category: Repository Management
capabilities:
  - workspace-scanning
  - tech-stack-detection
  - architectural-mapping
tags:
  - onboarding
  - context-window
---

# Skill: Codebase Index File Generation

## Purpose
Analyze the repository structure, architecture, and tech stack to generate a standardized, highly compact index file (e.g., `llms.txt` or `REPOS_INDEX.md`). This allows other AI agents to rapidly understand the repository landscape within their context windows.

## Capabilities & Scope
* Scan repository directory trees while respecting `.gitignore`.
* Identify core technology stacks and project types automatically.
* Map critical execution paths, entry points, and domain boundaries.
* Document local codebase rules, conventions, and style constraints.

## Prerequisites & Dependencies
* Access to workspace directory mapping commands (e.g., `tree`, `find`).
* Read access to dependency files (e.g., `package.json`, `requirements.txt`, `go.mod`).
* Read access to project configuration files (e.g., `tsconfig.json`, `docker-compose.yml`).

## Execution Protocol

### 1. Automated Discovery Phase
Execute the following bash script in the repository root to collect ground-truth context instantly. Save the output or use it to populate the template.

```bash
#!/usr/bin/env bash
echo "=== 1. HIGH-LEVEL DIRECTORY TREE ==="
if command -v tree &> /dev/null; then
    tree -I 'node_modules|.git|dist|.next|venv|build|out|target' -L 3
else
    find . -maxdepth 3 -not -path '*/.*' -not -path './node_modules*' -not -path './dist*' -not -path './.next*' -not -path './venv*' | sort | sed 's/[^ South-West-North-East]//g'
fi

echo -e "\n=== 2. DETECTED TECH STACK CONFIGURATIONS ==="
files_to_check=(
    "package.json" "requirements.txt" "go.mod" "Cargo.toml" "Gemfile" 
    "tsconfig.json" "docker-compose.yml" "next.config.js" "vite.config.ts"
)
for file in "${files_to_check[@]}"; do
    if [ -f "$file" ]; then
        echo "Found config: $file"
        head -n 15 "$file" # Peak config settings
        echo "------------------"
    fi
done

echo -e "\n=== 3. POTENTIAL ENTRY POINTS ==="
find . -maxdepth 3 -type f \( -name "main.py" -o -name "index.ts" -o -name "index.js" -o -name "app.ts" -o -name "app.js" -o -name "layout.tsx" -o -name "main.go" \) -not -path '*/.*' -not -path './node_modules*'
```

### 2. Synthesizing Metadata (Inference Required)
* Determine the primary 1-sentence purpose of the application based on the files found.
* List the top 3-5 core technical stack components.
* Group files logically by system layer (e.g., Components, Business Logic, API Routing, Data/DB Layer).

### 3. Document Generation
* Output a clean Markdown document adhering strictly to the Template below.
* Keep descriptions ultra-concise (10 words or fewer per file mapping).
* Ensure instructions are highly scannable with bullet points and bolding.

---

## Output Template

Generate the final index file exactly according to this layout structure:

````
# Codebase Index

> [Insert 1-sentence high-level description of what the project does]

## 🛠️ Tech Stack & Context
* **Core Framework:** [e.g., Next.js 14 App Router / FastAPI]
* **Language:** [e.g., TypeScript / Python 3.11]
* **State/Database:** [e.g., Prisma + PostgreSQL / Zustand]
* **Target Environment:** [e.g., Vercel / Docker Container]

## 🗺️ High-Level Directory Tree
```text
[Insert clean text tree structure collected from Step 1 here]
```

## 📂 Key Component Mappings
* **`[Path/to/Entry]`**: Application bootstrap and initialization point.
* **`[Path/to/Routes]`**: Handles incoming API requests and endpoints.
* **`[Path/to/Components]`**: Reusable UI elements and view logic.
* **`[Path/to/Models]`**: Database schema definitions and validations.
* **`[Path/to/Utilities]`**: Shared helper functions and global configurations.

## 🔄 Critical Execution Paths
* **Authentication Flow:** `[Path/to/Auth]` ➡️ `[Path/to/Middleware]` ➡️ Protected Routes.
* **Data Hydration Flow:** API Layer ➡️ Service Layer ➡️ UI Component.

## 📜 Local Codebase Rules
1. **File Naming:** Use [e.g., kebab-case / PascalCase] for all new files.
2. **State Management:** Mutate state strictly via [e.g., Server Actions / Redux Dispatches].
3. **Error Handling:** Wrap all API routes in global try-catch middleware; never return naked 500 errors.
4. **Testing Requirement:** Every new feature folder must contain a `__tests__/` directory with matching test suites.
````