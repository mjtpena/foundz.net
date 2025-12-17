# Foundz.Net - Azure AI Foundry CLI Agent

A production-ready CLI coding agent that integrates with Azure AI Foundry, supporting multiple AI models (Claude, GPT-4, Llama, Mistral, Cohere) with full agentic capabilities including autonomous tool use, codebase understanding, and git workflow automation.

## 🚀 Features

- **Multi-Model Support**: Claude, GPT-4, Llama, Mistral, Cohere via Azure AI Foundry
- **Agentic Architecture**: Autonomous tool use with planning and reasoning loops
- **Rich Tool System**: 40+ tools for file operations, git, code analysis, testing, and more
- **Interactive CLI**: Beautiful terminal UI with Spectre.Console
- **Streaming Responses**: Real-time token streaming for responsive interactions
- **Session Management**: Persistent conversation history with SQLite
- **Production Quality**: Enterprise-grade error handling, logging, monitoring, security

## 📋 Requirements

- .NET 10.0 or higher
- Azure AI Foundry endpoint and API key
- Git (for git operations)

## 🔧 Installation

### Via .NET Tool (Recommended)
```bash
dotnet tool install -g Foundz.Net
```

### From Source
```bash
git clone https://github.com/yourusername/foundz.net.git
cd foundz.net
dotnet build
dotnet run --project src/Foundz.Net.Cli
```

## 🎯 Quick Start

### Configure Azure AI Foundry
```bash
foundz config set azure.endpoint "https://your-endpoint.azure.com"
foundz config set azure.apiKey "your-api-key"
foundz config set azure.defaultModel "claude-3-5-sonnet-20241022"
```

### Start Interactive Chat
```bash
foundz chat
```

### Execute One-Shot Task
```bash
foundz task "Create a new C# class for user authentication"
```

### Review Git Changes
```bash
foundz diff
```

## 📖 Commands

- `chat` - Start interactive chat session
- `task` - Execute one-shot task
- `review` - Code review with AI assistance
- `diff` - Review git changes with AI
- `commit` - Generate commit messages
- `config` - Manage configuration
- `model` - List and test models
- `session` - Manage sessions

## 🛠️ Architecture

### Projects
- **Foundz.Net.Cli** - CLI entry point and command handlers
- **Foundz.Net.Core** - Business logic and agent orchestration
- **Foundz.Net.Tools** - Tool implementations (40+ tools)
- **Foundz.Net.Data** - Data layer with EF Core
- **Foundz.Net.Infrastructure** - Cross-cutting concerns
- **Foundz.Net.Shared** - Shared types and interfaces

### Key Technologies
- **Azure.AI.Inference** - Azure AI Foundry SDK
- **Microsoft.SemanticKernel** - Agent framework
- **System.CommandLine** - CLI framework
- **Spectre.Console** - Rich terminal UI
- **LibGit2Sharp** - Git operations
- **Entity Framework Core** - Data persistence
- **Polly** - Resilience patterns
- **Serilog** - Structured logging

## 🧰 Available Tools

### File Operations (10 tools)
- `read_file` - Read file contents with line numbers
- `write_file` - Write or create files
- `edit_file` - Edit specific lines
- `list_directory` - List directory contents
- `search_files` - Search for files and content
- More...

### Git Operations (8 tools)
- `git_status` - Repository status
- `git_diff` - View changes
- `git_commit` - Create commits
- `git_branch` - Branch management
- More...

### Code Analysis (6 tools)
- `parse_code` - Syntax analysis
- `find_references` - Symbol references
- `analyze_complexity` - Metrics
- More...

### Shell Execution (3 tools)
- `execute_command` - Run shell commands (whitelisted)
- `run_tests` - Execute test suites
- `build_project` - Build with auto-detection

## 🔒 Security

- Sandboxed tool execution
- Confirmation prompts for dangerous operations
- Command whitelist/blacklist
- Resource limits (CPU, memory, time)
- Audit logging
- Secret management with Azure Key Vault

## 📊 Configuration

Configuration is hierarchical from multiple sources:
1. Command-line arguments
2. Environment variables (`FOUNDZ_*`)
3. Project config (`.foundz/config.json`)
4. User config (`~/.foundz/config.json`)
5. Defaults

Example configuration:
```json
{
  "azure": {
    "endpoint": "https://your-endpoint.azure.com",
    "defaultModel": "claude-3-5-sonnet-20241022",
    "timeout": 120,
    "maxRetries": 3
  },
  "agent": {
    "maxIterations": 15,
    "maxContextTokens": 150000,
    "parallelToolExecution": true
  },
  "tools": {
    "confirmations": {
      "fileWrite": true,
      "fileDelete": true,
      "shellExecution": true
    }
  }
}
```

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run unit tests
dotnet test tests/Foundz.Net.Tests.Unit

# Run integration tests
dotnet test tests/Foundz.Net.Tests.Integration

# Run performance tests
dotnet test tests/Foundz.Net.Tests.Performance
```

## 📝 Development

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run --project src/Foundz.Net.Cli
```

### Add Database Migration
```bash
dotnet ef migrations add MigrationName --project src/Foundz.Net.Data
```

## 🤝 Contributing

Contributions are welcome! Please read the contributing guidelines first.

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- Azure AI Foundry team
- Microsoft Semantic Kernel team
- Spectre.Console contributors
- LibGit2Sharp contributors

## 📞 Support

- Documentation: https://docs.foundz.net
- Issues: https://github.com/yourusername/foundz.net/issues
- Discussions: https://github.com/yourusername/foundz.net/discussions

---

**Status**: 🚧 Active Development - Phase 1 Complete

**Current Version**: 0.1.0-alpha

**Target**: Production-ready v1.0 by Q2 2025
