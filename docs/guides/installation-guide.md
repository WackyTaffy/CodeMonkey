# Installation Guide

This guide will walk you through the process of installing and running CodeMonkey on your machine.

## Prerequisites

Before installing CodeMonkey, ensure you have a compatible LLM server running (such as `llama.cpp` as described in the [README](README.md)).

## Installation Steps

### 1. Download the Package
Download the latest release zip file from the following location:
`releases\2026-06-25\CodeMonkey.2026-06-25.zip`

### 2. Extract the Files
Extract the contents of the zip file to a folder where you want the application to reside. 
*Example path:* `C:\CodingTools\CodeMonkey`

### 3. Run the Application
To start CodeMonkey, you need to run it from its installation directory:

1. Open your preferred terminal (PowerShell, CMD, or Bash).
2. Navigate to the folder where you extracted the application:
   ```powershell
   cd C:\CodingTools\CodeMonkey
   ```
3. Run the executable file:
   ```powershell
   .\CodeMonkey.Cli.exe
   ```

## Troubleshooting

- **Missing DLLs:** Ensure you extracted all files from the zip package into the same directory.
- **Connection Errors:** If the application cannot connect to the LLM, verify that your `llama-server` is running and listening on the correct port (default: 8080) and host (0.0.0.0).
- **Permissions:** If you encounter permission errors when writing files, ensure you are running the terminal with the necessary privileges for the workspace you are targeting.
