# ⚡ Foundz.Net - Quick Start Guide

**Get up and running in 2 minutes!**

---

## 📦 What You Have

✅ Complete Azure AI Foundry CLI Agent foundation  
✅ 9 projects, ~2,000 lines of C# code  
✅ 4 working tools, agentic orchestrator  
✅ Beautiful CLI with Spectre.Console  
✅ Production-grade architecture  

---

## 🚀 Run It Now

```bash
# Navigate to project
cd /Users/mjtpena/dev/foundz.net

# Build (first time)
dotnet build

# Run the demo
dotnet run --project src/Foundz.Net.Cli
```

**Expected Output:**
```
_____                               _             _   _          _   
 |  ___|   ___    _   _   _ __     __| |  ____     | \ | |   ___  | |_ 
 | |_     / _ \  | | | | | '_ \   / _` | |_  /     |  \| |  / _ \ | __|
 |  _|   | (_) | | |_| | | | | | | (_| |  / /   _  | |\  | |  __/ | |_ 
 |_|      \___/   \__,_| |_| |_|  \__,_| /___| (_) |_| \_|  \___|  \__|

Azure AI Foundry CLI Agent - Phase 1 Complete
🚀 Foundz.Net Architecture Demonstration
✓ Registered 4 tools
[Beautiful table showing 4 implemented tools]
```

---

## 📖 Essential Reading (5 minutes)

**In this order:**

1. **SUMMARY.md** (2 min)
   - What's delivered
   - What's next
   - Quick overview

2. **README.md** (2 min)
   - Features
   - Architecture
   - Commands

3. **PROJECT_STATUS.md** (10 min)
   - Detailed status
   - Component breakdown
   - Roadmap

4. **IMPLEMENTATION_GUIDE.md** (Reference)
   - Step-by-step Phase 2 guide
   - Use when developing

---

## 🛠️ Key Commands

```bash
# Build everything
dotnet build

# Run the CLI
dotnet run --project src/Foundz.Net.Cli

# Run tests (when you add them)
dotnet test

# Restore packages
dotnet restore

# Clean build artifacts
dotnet clean

# Add a new package
dotnet add src/Foundz.Net.Core/Foundz.Net.Core.csproj package PackageName
```

---

## 📂 Key Files to Know

```
foundz.net/
├── README.md                    ← Start here
├── PROJECT_STATUS.md            ← Detailed status
├── IMPLEMENTATION_GUIDE.md      ← Development guide
├── SUMMARY.md                   ← Quick overview
├── DELIVERY.md                  ← Official delivery doc
└── QUICK_START.md              ← You are here!

src/
├── Foundz.Net.Shared/          ← Domain models & interfaces
│   ├── Models/                 ← Message, Session, etc.
│   └── Interfaces/             ← IAIClient, ITool, etc.
├── Foundz.Net.Core/            ← Business logic
│   ├── Agent/                  ← AgentOrchestrator, ToolRegistry
│   └── AI/                     ← AzureAIClient (stub)
├── Foundz.Net.Tools/           ← Tool implementations
│   ├── File/                   ← read_file, write_file, list_directory
│   └── Git/                    ← git_status
├── Foundz.Net.Data/            ← EF Core entities
│   ├── Entities/               ← SessionEntity, MessageEntity, etc.
│   └── Context/                ← FoundzDbContext
├── Foundz.Net.Infrastructure/  ← Configuration, logging
│   └── Configuration/          ← Config models
└── Foundz.Net.Cli/             ← Entry point
    └── Program.cs              ← Beautiful demo UI

tests/
├── Foundz.Net.Tests.Unit/      ← Unit tests (structure ready)
├── Foundz.Net.Tests.Integration/ ← Integration tests (structure ready)
└── Foundz.Net.Tests.Performance/ ← Performance tests (structure ready)
```

---

## 🎯 What Works Right Now

### ✅ Agent Orchestrator
```csharp
var orchestrator = new AgentOrchestrator(aiClient, toolRegistry, logger, config);
var response = await orchestrator.ProcessMessageAsync("List files", session);
// Executes agentic loop with tool calls
```

### ✅ Tool System
```csharp
var registry = new ToolRegistry(logger);
registry.RegisterTool(new ReadFileTool());
registry.RegisterTool(new WriteFileTool());

var tool = registry.GetTool("read_file");
var result = await tool.ExecuteAsync(new Dictionary<string, object>
{
    ["path"] = "README.md"
});
```

### ✅ Beautiful CLI
```csharp
AnsiConsole.Write(new FigletText("Foundz.Net"));
var table = new Table();
// Rich terminal output with Spectre.Console
```

---

## 🔧 Add Your First Tool (5 minutes)

**Step 1:** Create file `src/Foundz.Net.Tools/File/DeleteFileTool.cs`

```csharp
using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;

namespace Foundz.Net.Tools.File;

public class DeleteFileTool : ITool
{
    public string Name => "delete_file";
    public string Description => "Delete a file";
    public ToolCategory Category => ToolCategory.FileOperations;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Danger;
    
    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": { "type": "string", "description": "File to delete" }
            },
            "required": ["path"]
        }
        """;
    
    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken ct = default)
    {
        return Task.FromResult(args.ContainsKey("path"));
    }
    
    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken ct = default)
    {
        var path = args["path"].ToString()!;
        
        try
        {
            System.IO.File.Delete(path);
            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = $"Deleted: {path}",
                ExecutionTimeMs = 0
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = false,
                Error = ex.Message,
                ExecutionTimeMs = 0
            });
        }
    }
    
    public IEnumerable<string> GetExamples()
    {
        return ["delete_file({\"path\": \"temp.txt\"})"];
    }
}
```

**Step 2:** Register in `Program.cs`

```csharp
registry.RegisterTool(new DeleteFileTool());
```

**Step 3:** Build and run

```bash
dotnet build
dotnet run --project src/Foundz.Net.Cli
```

**Result:** See your new tool in the table! 🎉

---

## 📊 Quick Stats

| Metric | Value |
|--------|-------|
| **Projects** | 9 |
| **Lines of Code** | ~2,000 |
| **Tools** | 4 (10%) |
| **Interfaces** | 5 |
| **Models** | 7 |
| **Build Time** | ~1 second |
| **Status** | ✅ Working |

---

## 🚦 Status at a Glance

| Component | Status | Notes |
|-----------|--------|-------|
| Solution Structure | ✅ 100% | All 9 projects |
| Domain Models | ✅ 100% | 7 models defined |
| Interfaces | ✅ 100% | 5 interfaces |
| Agent Orchestrator | ✅ 100% | Agentic loop complete |
| Tool Registry | ✅ 100% | Dynamic registration |
| Tools | ⏳ 10% | 4 of 40 done |
| AI Client | ⚠️ Stub | Needs Azure endpoint |
| Data Layer | ✅ 100% | EF Core ready |
| CLI | ✅ 100% | Beautiful demo |
| Tests | ⏳ 0% | Structure ready |

**Legend:** ✅ Done | ⏳ In Progress | ⚠️ Needs Input

---

## 🎯 Your Next 3 Steps

### 1. Explore (10 minutes)
```bash
# Run the demo
dotnet run --project src/Foundz.Net.Cli

# Browse the code
code /Users/mjtpena/dev/foundz.net

# Read PROJECT_STATUS.md
```

### 2. Understand (30 minutes)
- Read through core interfaces in `Foundz.Net.Shared`
- Study `AgentOrchestrator.cs` to see agentic loop
- Check out existing tools in `Foundz.Net.Tools`
- Review architecture diagrams in docs

### 3. Extend (1 hour)
- Add a new tool (see example above)
- Write a unit test for a tool
- Implement a git tool (GitDiffTool)
- Experiment with Spectre.Console

---

## 🆘 Need Help?

### Common Issues

**Issue:** `dotnet: command not found`
```bash
# Verify .NET is in PATH
export PATH="/usr/local/share/dotnet:$PATH"
dotnet --version  # Should show 10.0.101
```

**Issue:** Build fails
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

**Issue:** Can't find a file
```bash
# List all C# files
find . -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*"
```

### Documentation

- **Architecture questions?** → `PROJECT_STATUS.md`
- **Implementation questions?** → `IMPLEMENTATION_GUIDE.md`
- **Quick overview?** → `SUMMARY.md`
- **What's delivered?** → `DELIVERY.md`
- **This guide** → `QUICK_START.md`

---

## 💡 Pro Tips

1. **Use GitHub Copilot**
   - Prompts are in `IMPLEMENTATION_GUIDE.md`
   - Reference existing tools as examples
   - Ask for tests with implementation

2. **Follow the Pattern**
   - All tools follow same structure
   - All async methods use CancellationToken
   - All errors return ToolResult with Success=false

3. **Test Incrementally**
   - Build after each file
   - Run frequently to catch issues early
   - Write tests alongside code

4. **Keep It Clean**
   - Follow existing naming conventions
   - Add XML docs to public APIs
   - Use nullable reference types

---

## 🎉 You're Ready!

You have:
- ✅ Working foundation
- ✅ Clear architecture
- ✅ Example implementations
- ✅ Comprehensive guides
- ✅ Path to completion

**Now build something amazing!** 🚀

---

## 📞 Quick Reference

**Commands:**
```bash
dotnet build                  # Build solution
dotnet run --project [proj]   # Run project
dotnet test                   # Run tests
dotnet clean                  # Clean build
```

**Key Directories:**
```
src/Foundz.Net.Shared    # Start here for models
src/Foundz.Net.Core      # Business logic
src/Foundz.Net.Tools     # Add tools here
src/Foundz.Net.Cli       # Entry point
```

**Key Files:**
```
Program.cs               # Main entry point
AgentOrchestrator.cs    # Agentic loop
ToolRegistry.cs         # Tool management
AzureAIClient.cs        # AI integration
```

---

**Time to explore:** 10 minutes  
**Time to understand:** 30 minutes  
**Time to extend:** 1 hour  
**Time to Phase 2:** Whenever you're ready!

✨ **Happy Coding!** ✨
