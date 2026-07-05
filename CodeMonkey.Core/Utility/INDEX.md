# CodeMonkey.Core/Utility Index

> Helper classes and shared constants for the core system.

## 📐 Interaction Map
**`Utility`** ➡️ `Services` (Consumed by)

## ☢️ Blast Radius
- **LOW/MEDIUM**: Mostly isolated helpers, but changes to `PathGuard` or `ContextConstants` can impact security and token limits.

## 🚀 Primary Entry Points
- **`PathGuard.cs`**: Critical for ensuring file system security.
- **`ContextConstants.cs`**: Defines the boundaries of AI context.

## 📂 Utility Components
- **Security**: `PathGuard.cs`
- **LLM Optimization**: `GemmaTokenHelper.cs`, `ContextConstants.cs`

## 📜 Local Rules & Conventions
- Utilities should be stateless and primarily consist of static methods.
