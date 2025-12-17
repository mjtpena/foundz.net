# Foundz.Net - Implementation Guide

**For: GitHub Copilot and Developers**  
**Purpose: Complete Phase 2-8 Implementation**

---

## 🎯 How to Use This Guide

This guide provides **concrete, actionable instructions** for implementing each remaining component. Each section includes:

1. **Exact file locations**
2. **Code patterns to follow**
3. **Dependencies required**
4. **Testing strategy**
5. **Integration points**

Use GitHub Copilot with these prompts for maximum effectiveness.

---

## Phase 2: Azure AI Foundry Integration

### 2.1 Update AzureAIClient with Real Implementation

**File:** `src/Foundz.Net.Core/AI/AzureAIClient.cs`

**Prerequisites:**
- Azure AI Foundry endpoint URL
- API key
- Model deployment name

**GitHub Copilot Prompt:**
```
Update the AzureAIClient class to use Azure.AI.Inference SDK properly. 
Implement:
1. Real ChatCompletionsClient initialization
2. SendMessageAsync with actual API calls
3. StreamMessageAsync with proper streaming
4. Tool call extraction from responses
5. Retry logic with Polly
6. Error handling for rate limits and transient errors

Follow the pattern in the existing stub but use the actual Azure.AI.Inference v1.0.0-beta.5 API.
Include:
- Proper message formatting
- Token counting
- Finish reason detection
- Error categorization
```

**Test Command:**
```bash
export AZURE_ENDPOINT="your-endpoint"
export AZURE_API_KEY="your-key"
dotnet test tests/Foundz.Net.Tests.Integration/AIClientTests.cs
```

---

### 2.2 Implement Provider Adapters

**Files to Create:**
- `src/Foundz.Net.Core/AI/Adapters/AnthropicAdapter.cs`
- `src/Foundz.Net.Core/AI/Adapters/OpenAIAdapter.cs`
- `src/Foundz.Net.Core/AI/Adapters/MetaLlamaAdapter.cs`

**GitHub Copilot Prompt for AnthropicAdapter:**
```
Create an AnthropicAdapter class implementing IProviderAdapter.
The adapter should:
1. Convert Message objects to Anthropic's format (role + content blocks)
2. Convert ToolDefinition to Anthropic tool schema
3. Parse Anthropic responses back to AIResponse
4. Handle streaming chunks properly
5. Return accurate ModelCapabilities for Claude models

Anthropic specifics:
- System messages are separate parameter
- Supports thinking/reasoning blocks
- Tool use in content blocks
- 200K token context for Claude 3.5
```

**Pattern:**
```csharp
public class AnthropicAdapter : IProviderAdapter
{
    public string ProviderName => "Anthropic";
    
    public object FormatMessages(List<Message> messages)
    {
        // Convert to Anthropic format
        // System messages go to separate parameter
        // Rest go to messages array
    }
    
    // Implement other methods...
}
```

---

## Phase 3: Complete Tool Suite

### 3.1 File Operations Tools

**Create these files in `src/Foundz.Net.Tools/File/`:**

1. **EditFileTool.cs**
```
Implement ITool for editing specific lines in a file.
Parameters: path, startLine, endLine, newContent
Features:
- Line range validation
- Backup before edit
- Syntax validation after edit (optional)
- Show diff
Danger level: Warning
```

2. **SearchFilesTool.cs**
```
Implement ITool for searching files by pattern and content.
Parameters: pattern (glob), contentRegex (optional), path (optional)
Features:
- Glob pattern matching
- Content regex search
- Return matches with context (3 lines before/after)
- Respect .gitignore
Danger level: Safe
```

3. **CreateDirectoryTool.cs**
```
Implement ITool for creating directories.
Parameters: path, recursive (default true)
Features:
- Create with parents
- Set permissions
- Return full path
- Idempotent operation
Danger level: Safe
```

**GitHub Copilot Pattern:**
```csharp
public class EditFileTool : ITool
{
    public string Name => "edit_file";
    public string Description => "Edit specific lines in a file";
    public ToolCategory Category => ToolCategory.FileOperations;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Warning;
    
    public string ParametersSchema => /* JSON schema */;
    
    public async Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> args, 
        CancellationToken cancellationToken)
    {
        // Implementation with error handling
    }
}
```

---

### 3.2 Git Operations Tools

**Create these files in `src/Foundz.Net.Tools/Git/`:**

Priority order:
1. **GitDiffTool.cs** - Show changes
2. **GitAddTool.cs** - Stage files
3. **GitCommitTool.cs** - Create commits
4. **GitBranchTool.cs** - Branch management
5. **GitCheckoutTool.cs** - Switch branches
6. **GitLogTool.cs** - View history
7. **GitPullTool.cs** - Pull from remote
8. **GitPushTool.cs** - Push to remote
9. **GitStashTool.cs** - Stash management

**GitHub Copilot Prompt Template:**
```
Create a {ToolName} class implementing ITool in the Git namespace.
Use LibGit2Sharp library.
Features:
- {Feature 1}
- {Feature 2}
- Proper error handling
- Return formatted output
- Set appropriate danger level
```

**Example for GitDiffTool:**
```csharp
public class GitDiffTool : ITool
{
    public async Task<ToolResult> ExecuteAsync(...)
    {
        using var repo = new Repository(repoPath);
        var diff = repo.Diff.Compare<TreeChanges>(/* params */);
        
        // Format diff output
        // Return as ToolResult
    }
}
```

---

### 3.3 Code Analysis Tools

**Prerequisites:**
```bash
dotnet add src/Foundz.Net.Tools package Microsoft.CodeAnalysis.CSharp
dotnet add src/Foundz.Net.Tools package Microsoft.CodeAnalysis.CSharp.Workspaces
```

**Create these files in `src/Foundz.Net.Tools/CodeAnalysis/`:**

1. **ParseCodeTool.cs**
```
Use Microsoft.CodeAnalysis.CSharp to parse C# code.
Extract:
- Classes, methods, properties
- Dependencies
- Complexity metrics
- Code smells
```

2. **FindReferencesTool.cs**
```
Find all references to a symbol across the codebase.
Use Roslyn semantic analysis.
Return list of locations with context.
```

3. **GetDefinitionTool.cs**
```
Navigate to definition of a symbol.
Use Roslyn symbol resolution.
Return definition location and signature.
```

4. **AnalyzeComplexityTool.cs**
```
Calculate cyclomatic and cognitive complexity.
Use Roslyn control flow analysis.
Return metrics with thresholds.
```

5. **DetectDuplicationTool.cs**
```
Find duplicated code blocks.
Use token-based analysis.
Return similarity percentage and locations.
```

6. **LintCodeTool.cs**
```
Run Roslyn analyzers.
Check for style violations.
Return warnings and suggestions.
```

**GitHub Copilot Prompt:**
```
Create a ParseCodeTool using Microsoft.CodeAnalysis.
Parse C# code and extract:
1. All type declarations (classes, interfaces, structs)
2. All member declarations
3. Using directives
4. Calculate basic metrics (LOC, method count)
5. Return as structured JSON

Use CSharpSyntaxTree.ParseText and semantic model.
Handle syntax errors gracefully.
```

---

## Phase 4: Infrastructure Completion

### 4.1 Configuration Loading

**File:** `src/Foundz.Net.Infrastructure/Configuration/ConfigurationLoader.cs`

**GitHub Copilot Prompt:**
```
Create a ConfigurationLoader class that:
1. Loads configuration from multiple sources (priority order):
   - Command-line arguments
   - Environment variables (FOUNDZ_*)
   - Project config (.foundz/config.json)
   - User config (~/.foundz/config.json)
   - Default values
2. Merges configurations hierarchically
3. Validates using FluentValidation
4. Returns FoundzConfiguration object

Use Microsoft.Extensions.Configuration with multiple providers.
```

**Pattern:**
```csharp
public class ConfigurationLoader
{
    public static FoundzConfiguration Load(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("/etc/foundz/config.json", optional: true)
            .AddJsonFile(Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile), ".foundz/config.json"), optional: true)
            .AddJsonFile(".foundz/config.json", optional: true)
            .AddEnvironmentVariables("FOUNDZ_")
            .AddCommandLine(args);
            
        var config = builder.Build();
        return config.Get<FoundzConfiguration>() ?? new FoundzConfiguration();
    }
}
```

---

### 4.2 Serilog Integration

**File:** `src/Foundz.Net.Infrastructure/Logging/LoggingConfiguration.cs`

**GitHub Copilot Prompt:**
```
Create a LoggingConfiguration class that sets up Serilog with:
1. Console sink with colored output
2. File sink with rolling logs
3. Structured logging
4. Log level configuration from settings
5. Enrichers (MachineName, ThreadId, ProcessId)
6. Sanitization of sensitive data (API keys, secrets)

Return ILoggerFactory for dependency injection.
```

---

### 4.3 Database Migrations

**Commands:**
```bash
cd src/Foundz.Net.Data
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**Verify migration:**
```bash
ls Migrations/
# Should see *_InitialCreate.cs and *_InitialCreate.Designer.cs
```

---

## Phase 5: Interactive Chat

### 5.1 Chat Command Handler

**File:** `src/Foundz.Net.Cli/Handlers/ChatHandler.cs`

**GitHub Copilot Prompt:**
```
Create a ChatHandler class that implements interactive chat:
1. Initialize agent orchestrator with AI client and tool registry
2. Create or resume session
3. Main loop:
   - Prompt for user input with Spectre.Console
   - Handle slash commands (/help, /exit, /tools, etc.)
   - Call agent orchestrator with streaming
   - Display streaming responses with smooth rendering
   - Show tool executions with progress indicators
   - Save messages to session
4. Handle Ctrl+C gracefully
5. Auto-save session on exit

Use AnsiConsole.Live for real-time updates during streaming.
```

**Pattern:**
```csharp
public class ChatHandler
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly ISessionRepository _sessions;
    
    public async Task RunAsync(ChatOptions options)
    {
        // Load or create session
        // Main loop
        // Handle commands
        // Stream responses
    }
}
```

---

### 5.2 Streaming Display

**File:** `src/Foundz.Net.Cli/UI/StreamingDisplay.cs`

**GitHub Copilot Prompt:**
```
Create a StreamingDisplay class using Spectre.Console.Live.
Features:
1. Smooth token-by-token display
2. Show current state (Thinking, Tool Execution, etc.)
3. Display tool calls with icons
4. Show progress bars for long operations
5. Update token count and cost in real-time
6. Syntax highlighting for code blocks

Use AnsiConsole.Live().StartAsync() for real-time updates.
```

---

## Phase 6: Testing

### 6.1 Unit Test Template

**File:** `tests/Foundz.Net.Tests.Unit/Tools/ReadFileToolTests.cs`

**GitHub Copilot Prompt:**
```
Create comprehensive unit tests for ReadFileTool:
1. Test reading existing file
2. Test reading non-existent file
3. Test reading with line range
4. Test reading binary file (should fail)
5. Test reading large file
6. Test with invalid parameters
7. Test cancellation

Use xUnit, Moq, and FluentAssertions.
Follow Arrange-Act-Assert pattern.
```

**Pattern:**
```csharp
public class ReadFileToolTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingFile_ReturnsSuccess()
    {
        // Arrange
        var tool = new ReadFileTool();
        var args = new Dictionary<string, object>
        {
            ["path"] = "test.txt"
        };
        
        // Create test file
        await File.WriteAllTextAsync("test.txt", "Hello, World!");
        
        // Act
        var result = await tool.ExecuteAsync(args);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("Hello, World!");
        
        // Cleanup
        File.Delete("test.txt");
    }
}
```

---

### 6.2 Integration Test Template

**File:** `tests/Foundz.Net.Tests.Integration/EndToEndTests.cs`

**GitHub Copilot Prompt:**
```
Create end-to-end integration test:
1. Initialize full system (AI client, orchestrator, tools, db)
2. Start a chat session
3. Send a message: "List files in current directory"
4. Verify tool execution (list_directory called)
5. Check response contains file list
6. Verify session saved to database
7. Resume session
8. Send another message
9. Verify continuation works

Use test containers for database if needed.
```

---

## Phase 7: Advanced Features

### 7.1 Codebase Indexer

**File:** `src/Foundz.Net.Core/Memory/CodebaseIndexer.cs`

**GitHub Copilot Prompt:**
```
Create a CodebaseIndexer class that:
1. Walks project directory recursively
2. Respects .gitignore and .aiaignore
3. Extracts symbols using Roslyn (for C#) and tree-sitter (for others)
4. Builds dependency graph
5. Calculates file importance scores
6. Stores index in SQLite with FTS5
7. Provides search by:
   - File name
   - Symbol name
   - Content
   - Type
8. Implements file watching for incremental updates
9. Returns most relevant files for a query

Use:
- FileSystemWatcher for real-time updates
- Microsoft.CodeAnalysis for C# parsing
- SQLite FTS5 for full-text search
```

---

### 7.2 Context Selection

**File:** `src/Foundz.Net.Core/Memory/ContextSelector.cs`

**GitHub Copilot Prompt:**
```
Create a ContextSelector that chooses relevant files for a query:
Algorithm:
1. Parse query for file names, symbols, keywords
2. Search index for matches
3. Score files by:
   - Keyword relevance (TF-IDF)
   - Recency (recently modified = higher score)
   - Dependency proximity (imports/imported by relevant files)
   - User interaction history
4. Apply token budget constraint (leave 30% for response)
5. Return ordered list of files to include

Target: Select 5-15 most relevant files within token budget.
```

---

### 7.3 Semantic Kernel Integration

**File:** `src/Foundz.Net.Core/Memory/SemanticMemory.cs`

**GitHub Copilot Prompt:**
```
Integrate Microsoft.SemanticKernel for:
1. Semantic search over codebase
2. Memory storage (facts, preferences, context)
3. Planning capabilities
4. Plugin system for custom functions

Create a SemanticMemory class that:
- Stores conversation facts
- Retrieves relevant memories based on query
- Integrates with agent orchestrator
- Uses embeddings for semantic search (optional)
```

---

## Phase 8: Production Polish

### 8.1 Error Recovery

**File:** `src/Foundz.Net.Core/Agent/ErrorRecovery.cs`

**GitHub Copilot Prompt:**
```
Create an ErrorRecovery class that handles:
1. AI API failures (retry with backoff)
2. Tool execution failures (suggest alternatives)
3. Token limit exceeded (truncate intelligently)
4. Rate limiting (wait and retry)
5. Network errors (queue requests)
6. State corruption (recover from last checkpoint)

Implement circuit breaker pattern for failing services.
```

---

### 8.2 Security Sandboxing

**File:** `src/Foundz.Net.Infrastructure/Security/Sandbox.cs`

**GitHub Copilot Prompt:**
```
Create a Sandbox class for safe tool execution:
1. File system restrictions (chroot-like, project directory only)
2. Command whitelist/blacklist
3. Resource limits:
   - CPU time
   - Memory usage
   - Disk I/O
   - Network access
4. Process isolation
5. Timeout enforcement

Use Process class with limits where possible.
Consider using Docker/containers for full isolation.
```

---

### 8.3 CI/CD Pipeline

**File:** `.github/workflows/build.yml`

**Template:**
```yaml
name: Build and Test

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --verbosity normal
      - run: dotnet publish -c Release -o publish
      - uses: actions/upload-artifact@v3
        with:
          name: foundz-cli
          path: publish/
```

---

## 🔍 Testing Strategy

### For Each Component:

1. **Unit Tests** (85%+ coverage)
   - Test happy paths
   - Test error cases
   - Test edge cases
   - Test cancellation
   - Mock external dependencies

2. **Integration Tests** (Key workflows)
   - End-to-end scenarios
   - Database interactions
   - API calls (with test endpoint)
   - File system operations

3. **Performance Tests** (Critical paths)
   - Tool execution speed
   - AI response latency
   - Index build time
   - Memory usage

---

## 📊 Success Criteria

### Phase 2 Complete When:
- ✅ Can make actual Azure AI Foundry API calls
- ✅ Streaming responses work end-to-end
- ✅ At least 2 provider adapters work (Claude + GPT-4)
- ✅ 20+ tools implemented and tested
- ✅ Configuration loads from files

### Phase 3-8 Complete When:
- ✅ All 40 tools implemented
- ✅ >85% unit test coverage
- ✅ Integration tests pass
- ✅ Interactive chat works smoothly
- ✅ Code analysis tools work on real codebases
- ✅ Documentation complete
- ✅ CI/CD pipeline running
- ✅ Ready for beta testing

---

## 💡 Pro Tips for GitHub Copilot

### 1. Be Specific
❌ Bad: "Create a tool"
✅ Good: "Create a GitDiffTool implementing ITool that shows git changes using LibGit2Sharp"

### 2. Provide Context
Include:
- Which interfaces to implement
- Which libraries to use
- Expected input/output format
- Error handling requirements

### 3. Request Tests
Always ask for tests with the implementation:
"Create XTool and comprehensive unit tests"

### 4. Iterate
Start with basic implementation, then enhance:
1. Basic functionality
2. Error handling
3. Edge cases
4. Performance optimization

### 5. Reference Existing Code
"Create GitCommitTool following the same pattern as GitStatusTool"

---

## 📚 Reference Documentation

- [Azure AI Inference SDK](https://learn.microsoft.com/azure/ai-studio/)
- [Microsoft.SemanticKernel](https://learn.microsoft.com/semantic-kernel/)
- [Roslyn API](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/)
- [LibGit2Sharp](https://github.com/libgit2/libgit2sharp/wiki)
- [Spectre.Console](https://spectreconsole.net/)
- [EF Core](https://learn.microsoft.com/ef/core/)

---

## 🚀 Getting Started

**Today's Task List (Next 2 Hours):**

1. ⏳ Set up Azure AI Foundry endpoint
2. ⏳ Update AzureAIClient with real implementation
3. ⏳ Test with simple prompt
4. ⏳ Implement AnthropicAdapter
5. ⏳ Create 3 more file tools

**This Week:**
- Complete AI integration
- Implement 20 tools
- Wire up configuration loading
- Create first set of unit tests

**This Month:**
- All tools complete
- Interactive chat working
- Code analysis functional
- Test coverage >80%

---

## 🎯 Final Note

The architecture is solid. The path is clear. Now it's execution time.

Use this guide, leverage GitHub Copilot, test incrementally, and ship progressively.

**Let's build something amazing! 🚀**

---

*End of Implementation Guide*
