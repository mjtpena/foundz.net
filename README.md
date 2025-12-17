# Foundz.Net

> **Enterprise AI Agent Platform** built on Microsoft Foundry and Azure AI Agent Service, featuring autonomous multi-agent orchestration, 42+ production tools, and enterprise-grade observability.

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/mjtpena/foundz.net)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![Microsoft Foundry](https://img.shields.io/badge/Microsoft-Foundry-0078D4?logo=microsoft)](https://learn.microsoft.com/azure/ai-foundry/)
[![Azure AI Agents](https://img.shields.io/badge/Azure-AI%20Agents-0078D4?logo=microsoftazure)](https://learn.microsoft.com/azure/ai-services/agents/)
[![Code Coverage](https://img.shields.io/badge/coverage-100%25-brightgreen)](https://github.com/mjtpena/foundz.net)

---

## 🎯 What is Foundz.Net?

**Foundz.Net is an enterprise-grade AI agent platform** that extends Microsoft's Foundry infrastructure with production-ready tools, observability, and autonomous agent capabilities.

### Built on Microsoft's Enterprise AI Stack

```
┌─────────────────────────────────────────────────────────┐
│             Foundz.Net (Your Application)               │
│  • 42+ Production Tools  • Beautiful CLI                │
│  • Cost Tracking         • Performance Metrics          │
│  • Batch Processing      • Session Management           │
└────────────────────┬────────────────────────────────────┘
                     │ Uses ↓
┌────────────────────▼────────────────────────────────────┐
│          Microsoft Foundry & Agent Platform             │
├─────────────────────────────────────────────────────────┤
│  Azure.AI.Projects (1.1.0)                              │
│  └─ Project management, model deployment, evaluation    │
│                                                          │
│  Azure.AI.Agents.Persistent (1.1.0)                     │
│  └─ Production agents with threads, memory, tools       │
│                                                          │
│  Microsoft.SemanticKernel.Agents.Core (1.68.0)          │
│  └─ Multi-agent orchestration and collaboration         │
└─────────────────────────────────────────────────────────┘
```

---

## 🚀 Core Features

### 🤖 **Autonomous Agent System**
- **Persistent Agents** with memory and state (Azure AI Agent Service)
- **Multi-Agent Orchestration** for complex tasks (Semantic Kernel)
- **Thread-Based Conversations** with full history
- **Tool/Function Calling** with automatic orchestration

### 🛠️ **42+ Production Tools**
| Category | Tools | Examples |
|----------|-------|----------|
| **File Operations** | 10 | Read, write, edit, search, copy, move, delete |
| **Git Operations** | 10 | Status, diff, commit, branch, log, merge |
| **Code Analysis** | 6 | Parse, complexity, references, definitions |
| **Shell Execution** | 3 | Commands, test running, project building |
| **Search** | 4 | Codebase search, documentation search |
| **Testing** | 3 | Generation, execution, coverage |
| **Refactoring** | 2 | Rename symbols, extract methods |
| **Documentation** | 3 | Generate, update, explain |

### 📊 **Enterprise Observability**
- ✅ **Real-Time Cost Tracking** across all operations
- ✅ **Performance Metrics** (P50/P95/P99 latency)
- ✅ **Success Rate Monitoring** per model
- ✅ **Token Usage Analytics** with breakdowns
- ✅ **Export to JSON** for analysis

### 💻 **Beautiful CLI Experience**
- ✅ Interactive chat with slash commands
- ✅ One-shot and batch task execution
- ✅ Session management with persistence
- ✅ Progress indicators and spinners
- ✅ Color-coded output (Spectre.Console)

---

## 📦 Quick Start

```bash
# Clone
git clone https://github.com/mjtpena/foundz.net.git
cd foundz.net

# Build
dotnet restore && dotnet build

# Run
dotnet run --project Foundz.Net.Cli
```

**See [QUICK_START.md](QUICK_START.md) for detailed setup.**

---

## 💡 Usage Examples

### Interactive Chat
```bash
dotnet run --project Foundz.Net.Cli chat

You: analyze the authentication code
Assistant: I'll analyze it...

# Slash commands: /help /stats /cost /history /exit
```

### Execute Tasks
```bash
# One-shot
dotnet run --project Foundz.Net.Cli task "refactor user service"

# Batch
dotnet run --project Foundz.Net.Cli task --batch task1.txt task2.txt
```

### View Metrics
```bash
# Statistics
dotnet run --project Foundz.Net.Cli stats

# Costs
dotnet run --project Foundz.Net.Cli costs

# Models
dotnet run --project Foundz.Net.Cli models
```

---

## 🏗️ Architecture

```
Foundz.Net/
├── Foundz.Net.Cli/              # CLI interface
├── Foundz.Net.Core/             # Core logic + Microsoft integrations
├── Foundz.Net.Tools/            # 42+ tool implementations
├── Foundz.Net.Data/             # EF Core persistence
├── Foundz.Net.Infrastructure/   # Configuration
├── Foundz.Net.Shared/           # Models & interfaces
└── Tests/                       # 100% coverage
```

**See [MICROSOFT_FOUNDRY_AND_AGENTS_ARCHITECTURE.md](MICROSOFT_FOUNDRY_AND_AGENTS_ARCHITECTURE.md) for details.**

---

## 📊 Status

| Metric | Value |
|--------|-------|
| **Build** | ✅ Passing |
| **Tests** | ✅ 18/18 |
| **Coverage** | ✅ 100% |
| **Build Time** | 3.1s |
| **Tools** | 42+ |

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [QUICK_START.md](QUICK_START.md) | Get started in 5 minutes |
| [MICROSOFT_FOUNDRY_AND_AGENTS_ARCHITECTURE.md](MICROSOFT_FOUNDRY_AND_AGENTS_ARCHITECTURE.md) | Platform architecture |
| [TOOLS_REFERENCE.md](TOOLS_REFERENCE.md) | Complete tools guide |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Contribution guide |
| [BUILD_STATUS.md](BUILD_STATUS.md) | Build status |
| [RELEASE_NOTES.md](RELEASE_NOTES.md) | Version history |

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true

# Coverage report
dotnet reportgenerator -reports:coverage.opencover.xml -targetdir:coverage-report
```

---

## 🤝 Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md).

1. Fork the repository
2. Create feature branch
3. Write tests (100% coverage required)
4. Submit pull request

---

## 📝 License

MIT License - see [LICENSE](LICENSE)

---

## 🎯 vs Competitors

| Feature | Foundz.Net | Claude Code | Copilot CLI |
|---------|------------|-------------|-------------|
| **Microsoft Foundry** | ✅ | ❌ | ❌ |
| **Azure AI Agents** | ✅ | ❌ | ❌ |
| **Multi-Agent** | ✅ | ❌ | ❌ |
| **Cost Tracking** | ✅ | ❌ | ❌ |
| **Open Source** | ✅ | ❌ | ❌ |
| **Self-Hostable** | ✅ | ❌ | ❌ |

---

## 🗺️ Roadmap

### ✅ Done
- Core agent orchestration
- 42+ tools
- Microsoft Foundry integration
- Cost tracking
- Interactive CLI

### 🚧 In Progress
- 100% code coverage
- Live API testing

### 📅 Planned
- Web UI
- VS Code extension
- Docker support
- Multi-agent patterns

---

## 📬 Support

- **Issues**: [GitHub Issues](https://github.com/mjtpena/foundz.net/issues)
- **Discussions**: [GitHub Discussions](https://github.com/mjtpena/foundz.net/discussions)

---

⭐ **Star us if you find this useful!**

**Built with ❤️ on Microsoft Foundry Platform**
