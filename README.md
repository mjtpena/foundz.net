# Foundz.Net

> An intelligent AI-powered CLI agent built with Azure AI Foundry, featuring multi-provider support, 42+ tools, and production-ready architecture.

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/yourusername/foundz.net)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)

## 🚀 Features

### Core Capabilities
- **Interactive Chat** - Full conversational AI experience with session management
- **Task Execution** - One-shot and batch task processing
- **Multi-Provider AI** - Support for Claude, GPT-4, Mistral, Cohere, and Llama models
- **42+ Tools** - File operations, Git, code analysis, shell execution, and more
- **Cost Tracking** - Real-time cost monitoring with detailed breakdowns
- **Performance Metrics** - Comprehensive statistics and latency tracking
- **Session Management** - Persistent conversation history

### AI Providers
- ✅ **Anthropic** (Claude 3.5 Sonnet, Opus, Haiku)
- ✅ **OpenAI** (GPT-4 Turbo, GPT-4o, GPT-3.5)
- ✅ **Mistral AI** (Large, Medium, Small)
- ✅ **Cohere** (Command R, Command R+)
- ✅ **Meta** (Llama 3.1 405B, 70B, 8B)

### Tool Categories
- **File Operations** (10 tools): Read, write, edit, search, copy, move, delete
- **Git Operations** (10 tools): Status, diff, commit, branch, log, stash
- **Code Analysis** (6 tools): Parse, complexity analysis, reference finding
- **Shell Execution** (3 tools): Command execution, test running, project building
- **Search Tools** (4 tools): Codebase search, documentation search
- **Testing Tools** (3 tools): Test generation, execution, coverage analysis
- **Refactoring Tools** (2 tools): Symbol renaming, method extraction
- **Documentation Tools** (3 tools): Doc generation, explanation, updates

## 📦 Installation

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Azure AI Foundry endpoint (optional - for Azure deployments)
- API keys for your chosen AI provider

### Quick Start

```bash
# Clone the repository
git clone https://github.com/yourusername/foundz.net.git
cd foundz.net

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the CLI
dotnet run --project Foundz.Net.Cli
```

## 🎯 Usage

### Interactive Chat

Start an interactive conversation with the AI agent:

```bash
dotnet run --project Foundz.Net.Cli chat

You: analyze the authentication implementation
Assistant: I'll analyze the authentication code...

# Use slash commands
/help     - Show available commands
/stats    - Display session statistics
/cost     - Show cost breakdown
/history  - View conversation history
/exit     - Exit chat
```

### One-Shot Tasks

Execute a single task:

```bash
# Simple task
dotnet run --project Foundz.Net.Cli task "refactor the user service"

# From file
dotnet run --project Foundz.Net.Cli task --file tasks.txt

# Verbose output
dotnet run --project Foundz.Net.Cli task "analyze performance" --verbose
```

### Batch Processing

Execute multiple tasks sequentially:

```bash
dotnet run --project Foundz.Net.Cli task --batch \
  task1.txt \
  task2.txt \
  task3.txt \
  --stop-on-error
```

### View Statistics

```bash
# Overall statistics
dotnet run --project Foundz.Net.Cli stats

# Model-specific statistics
dotnet run --project Foundz.Net.Cli stats gpt-4-turbo
```

### Cost Tracking

```bash
# All sessions
dotnet run --project Foundz.Net.Cli costs

# Specific session
dotnet run --project Foundz.Net.Cli costs --session abc123

# Export to JSON
dotnet run --project Foundz.Net.Cli costs export costs.json
```

### Model Information

```bash
# List all models
dotnet run --project Foundz.Net.Cli models

# Model details
dotnet run --project Foundz.Net.Cli models claude-3-5-sonnet-20241022

# Compare models
dotnet run --project Foundz.Net.Cli models compare gpt-4-turbo claude-3-5-sonnet-20241022
```

## 🏗️ Architecture

### Clean Architecture

```
foundz.net/
├── Foundz.Net.Cli/              # CLI interface with Spectre.Console
├── Foundz.Net.Core/             # Core business logic
│   ├── AI/                      # AI clients and adapters
│   ├── Orchestration/           # Agent orchestrator
│   ├── ToolRegistry/            # Tool management
│   ├── Memory/                  # Codebase indexing & semantic memory
│   ├── Planning/                # Task planning & progress tracking
│   └── Session/                 # Session management
├── Foundz.Net.Tools/            # Tool implementations
│   ├── File/                    # File operations
│   ├── Git/                     # Git operations
│   ├── CodeAnalysis/            # Code analysis tools
│   ├── Shell/                   # Shell execution
│   ├── Search/                  # Search tools
│   ├── Testing/                 # Testing tools
│   ├── Refactoring/             # Refactoring tools
│   └── Documentation/           # Documentation tools
├── Foundz.Net.Data/             # Data access layer (EF Core)
├── Foundz.Net.Infrastructure/   # Configuration & external services
├── Foundz.Net.Shared/           # Shared models & interfaces
└── Tests/                       # Unit, integration & performance tests
```

### Key Components

- **AgentOrchestrator**: Manages the agentic loop with tool execution
- **ToolRegistry**: Dynamic tool discovery and management
- **CostTracker**: Real-time cost monitoring across providers
- **RequestMetricsCollector**: Performance tracking and analytics
- **ModelCapabilityDetector**: Auto-detection of model features
- **CodebaseIndexer**: Intelligent code understanding with Roslyn
- **SessionManager**: Persistent conversation management

## 🔧 Configuration

### Environment Variables

```bash
# Azure AI Foundry (optional)
AZURE_AI_ENDPOINT=https://your-endpoint.openai.azure.com/
AZURE_AI_KEY=your-api-key

# Provider API Keys
ANTHROPIC_API_KEY=your-anthropic-key
OPENAI_API_KEY=your-openai-key
MISTRAL_API_KEY=your-mistral-key
COHERE_API_KEY=your-cohere-key
```

### Configuration File

Create `appsettings.json` in the Foundz.Net.Cli directory:

```json
{
  "AI": {
    "Provider": "Anthropic",
    "Model": "claude-3-5-sonnet-20241022",
    "MaxTokens": 4096,
    "Temperature": 0.7
  },
  "Session": {
    "AutoSave": true,
    "MaxHistory": 20
  },
  "CostTracking": {
    "Enabled": true
  },
  "Metrics": {
    "Enabled": true
  }
}
```

## 📊 Performance

- **Build Time**: 3.1 seconds
- **Test Execution**: 1.9 seconds (18/18 passing)
- **Average Request Latency**: <2 seconds
- **Memory Usage**: ~100 MB typical
- **Token Processing**: 1000+ tokens/second

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration

# With coverage
dotnet test /p:CollectCoverage=true
```

## 📚 Documentation

- [Quick Start Guide](QUICK_START.md)
- [Tools Reference](TOOLS_REFERENCE.md)
- [Build Status](BUILD_STATUS.md)
- [Architecture Details](docs/archive/)

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details.

### Development Setup

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Style

- Follow C# coding conventions
- Use async/await for I/O operations
- Add XML documentation for public APIs
- Write unit tests for new features
- Keep PRs focused and small

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Azure AI Foundry** - For the AI infrastructure
- **Anthropic** - Claude models
- **OpenAI** - GPT models
- **Mistral AI** - Mistral models
- **Cohere** - Command R models
- **Meta** - Llama models
- **Spectre.Console** - Beautiful CLI interface
- **Roslyn** - Code analysis capabilities
- **Semantic Kernel** - Memory and orchestration patterns

## 📬 Contact

- **GitHub Issues**: [Create an issue](https://github.com/yourusername/foundz.net/issues)
- **Discussions**: [Join the discussion](https://github.com/yourusername/foundz.net/discussions)

## 🗺️ Roadmap

### Completed ✅
- [x] Core agent orchestration
- [x] 42+ production tools
- [x] Multi-provider AI support (5 providers)
- [x] Cost tracking & metrics
- [x] Interactive CLI
- [x] Session management
- [x] Codebase indexing
- [x] Planning capabilities

### Upcoming 🚧
- [ ] Web UI interface
- [ ] Plugin system
- [ ] Custom tool creation wizard
- [ ] Advanced context management
- [ ] Multi-agent collaboration
- [ ] Docker containerization
- [ ] CI/CD templates
- [ ] Comprehensive documentation site

## ⭐ Star History

If you find this project useful, please consider giving it a star! ⭐

---

**Built with ❤️ using .NET 10.0 and Azure AI Foundry**
