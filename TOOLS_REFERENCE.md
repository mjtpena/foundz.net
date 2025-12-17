# Foundz.Net Tools Reference Guide

Complete reference for all 38 implemented tools across 8 categories.

---

## 📁 File Operations (10 tools)

### read_file
**Description**: Read file contents with optional line ranges  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `read_file({"path": "Program.cs", "startLine": 1, "endLine": 10})`

### write_file
**Description**: Create or overwrite files with automatic backup  
**Safety**: Warning  
**Requires Confirmation**: Yes  
**Example**: `write_file({"path": "output.txt", "content": "Hello World"})`

### edit_file
**Description**: Apply precise edits to files with validation  
**Safety**: Warning  
**Requires Confirmation**: Yes  
**Example**: `edit_file({"path": "Program.cs", "startLine": 10, "newContent": "// Updated"})`

### search_files
**Description**: Search for files using glob patterns or content  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `search_files({"pattern": "*.cs", "directory": "./src"})`

### list_directory
**Description**: List directory contents with filtering and sorting  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `list_directory({"path": "./src", "recursive": true})`

### create_directory
**Description**: Create directories with parent paths  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `create_directory({"path": "./src/NewFolder"})`

### delete_file
**Description**: Delete files or directories with safety checks  
**Safety**: Danger  
**Requires Confirmation**: Yes  
**Example**: `delete_file({"path": "temp.txt"})`

### rename_file
**Description**: Rename files or directories with conflict detection  
**Safety**: Warning  
**Requires Confirmation**: Yes  
**Example**: `rename_file({"oldPath": "old.txt", "newPath": "new.txt"})`

### copy_file
**Description**: Copy files or directories recursively  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `copy_file({"sourcePath": "src.txt", "destinationPath": "dest.txt"})`

### get_file_info
**Description**: Get detailed file metadata and statistics  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `get_file_info({"path": "Program.cs"})`

---

## 🌿 Git Operations (10 tools)

### git_status
**Description**: Show working directory status and changes  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `git_status({"repositoryPath": "."})`

### git_diff
**Description**: Show differences between commits or working tree  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `git_diff({"cached": true})`

### git_add
**Description**: Stage files for commit  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `git_add({"files": ["Program.cs"], "all": false})`

### git_commit
**Description**: Create a new commit with message  
**Safety**: Warning  
**Requires Confirmation**: Yes  
**Example**: `git_commit({"message": "feat: add new feature"})`

### git_branch
**Description**: List, create, or delete branches  
**Safety**: Warning  
**Requires Confirmation**: Yes (for delete)  
**Example**: `git_branch({"action": "create", "branchName": "feature/new"})`

### git_checkout
**Description**: Switch branches or restore files  
**Safety**: Warning  
**Requires Confirmation**: Yes  
**Example**: `git_checkout({"branchName": "main"})`

### git_log
**Description**: Show commit history with filtering  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `git_log({"maxCount": 10})`

### git_pull
**Description**: Fetch and merge from remote  
**Safety**: Warning  
**Requires Confirmation**: Yes  
**Example**: `git_pull({"rebase": true})`

### git_push
**Description**: Push commits to remote repository  
**Safety**: Danger  
**Requires Confirmation**: Yes  
**Example**: `git_push({"remote": "origin", "setUpstream": true})`

### git_stash
**Description**: Stash, list, apply, or drop changes  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `git_stash({"operation": "save", "message": "WIP"})`

---

## 🔍 Code Analysis (6 tools)

### parse_code
**Description**: Parse code into syntax tree with Roslyn  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `parse_code({"filePath": "Program.cs"})`

### find_references
**Description**: Find all references to a symbol  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `find_references({"symbolName": "MyClass", "projectPath": "Project.csproj"})`

### get_definition
**Description**: Get symbol definition and documentation  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `get_definition({"symbolName": "Calculate", "projectPath": "Project.csproj"})`

### analyze_complexity
**Description**: Calculate code complexity metrics  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `analyze_complexity({"filePath": "Service.cs"})`

### detect_duplication
**Description**: Find duplicated code blocks  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `detect_duplication({"directoryPath": "./src", "minLines": 6})`

### lint_code
**Description**: Run static analyzers and linters  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `lint_code({"projectPath": "Project.csproj", "severity": "Warning"})`

---

## 💻 Shell Execution (3 tools)

### execute_command
**Description**: Execute shell commands with safety whitelist  
**Safety**: Danger  
**Requires Confirmation**: Yes  
**Example**: `execute_command({"command": "dotnet", "arguments": ["--version"]})`

### run_tests
**Description**: Auto-detect and run tests with framework support  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `run_tests({"projectPath": "Tests.csproj", "filter": "UnitTests"})`

### build_project
**Description**: Build projects with configuration selection  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `build_project({"projectPath": "Project.csproj", "configuration": "Release"})`

---

## 🔎 Search (4 tools)

### search_codebase
**Description**: Full-text search across entire codebase  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `search_codebase({"query": "TODO", "filePattern": "*.cs", "useRegex": false})`

### search_documentation
**Description**: Search README, docs, and wiki files  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `search_documentation({"query": "installation", "maxResults": 20})`

### search_dependencies
**Description**: Analyze project dependencies and packages  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `search_dependencies({"packageName": "Microsoft.Extensions"})`

---

## 🧪 Testing (3 tools)

### generate_test
**Description**: Generate unit test scaffolding  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `generate_test({"filePath": "Calculator.cs", "className": "Calculator", "testFramework": "xUnit"})`

### analyze_coverage
**Description**: Parse and analyze code coverage reports  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `analyze_coverage({"coverageFile": "coverage.xml", "threshold": 80})`

---

## ♻️ Refactoring (2 tools)

### rename_symbol
**Description**: Rename symbols across entire codebase  
**Safety**: Warning  
**Requires Confirmation**: Yes  
**Example**: `rename_symbol({"projectPath": "Project.csproj", "oldName": "OldClass", "newName": "NewClass", "dryRun": true})`

### extract_method
**Description**: Extract code into a new method  
**Safety**: Warning  
**Requires Confirmation**: Yes  
**Example**: `extract_method({"filePath": "Service.cs", "startLine": 45, "endLine": 52, "methodName": "ValidateInput"})`

---

## 📚 Documentation (2 tools)

### generate_docs
**Description**: Generate XML documentation comments  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `generate_docs({"filePath": "Calculator.cs", "style": "XML"})`

### explain_code
**Description**: Generate natural language code explanations  
**Safety**: Safe  
**Requires Confirmation**: No  
**Example**: `explain_code({"filePath": "Program.cs", "detailLevel": "detailed"})`

---

## Tool Properties Reference

### Safety Levels
- **Safe**: No destructive operations, read-only or append-only
- **Warning**: Modifies files or state, but recoverable
- **Danger**: Potentially destructive, requires explicit confirmation

### Common Parameters

#### File Operations
- `path` (string): File or directory path
- `content` (string): File content for write operations
- `recursive` (boolean): Process directories recursively
- `overwrite` (boolean): Allow overwriting existing files

#### Git Operations
- `repositoryPath` (string): Path to git repository (defaults to current directory)
- `message` (string): Commit message
- `branchName` (string): Branch name
- `remote` (string): Remote name (default: "origin")

#### Code Analysis
- `projectPath` (string): Path to .csproj or .sln file
- `filePath` (string): Path to source code file
- `symbolName` (string): Name of symbol to analyze

#### Search Operations
- `query` (string): Search query or pattern
- `directory` (string): Directory to search in
- `maxResults` (integer): Maximum number of results
- `useRegex` (boolean): Use regex pattern matching

### Return Format

All tools return a `ToolResult` with:
```csharp
{
    "ToolCallId": "unique-id",
    "ToolName": "tool_name",
    "Success": true/false,
    "Output": "human-readable output",
    "Error": "error message if failed",
    "ExecutionTimeMs": 123,
    "Metadata": {
        "key": "value",
        // Tool-specific metadata
    }
}
```

---

## Integration Notes

### Tool Discovery
All tools implement `ITool` interface and are auto-discoverable via:
```csharp
var tools = typeof(ITool).Assembly
    .GetTypes()
    .Where(t => typeof(ITool).IsAssignableFrom(t) && !t.IsInterface)
    .Select(t => (ITool)Activator.CreateInstance(t))
    .ToList();
```

### JSON Schema
Each tool provides `ParametersSchema` in JSON Schema format for AI model function calling.

### Validation
All tools validate parameters in `ValidateArgsAsync` before execution.

### Cancellation
All tools support `CancellationToken` for cooperative cancellation.

### Examples
All tools provide usage examples via `GetExamples()` for documentation and testing.

---

## Usage Patterns

### Reading and Writing Files
```
1. read_file to get content
2. Analyze/modify content
3. write_file or edit_file to save changes
```

### Git Workflow
```
1. git_status to check changes
2. git_diff to review changes
3. git_add to stage files
4. git_commit to commit
5. git_push to remote
```

### Code Analysis Workflow
```
1. parse_code to understand structure
2. find_references to track usage
3. get_definition for details
4. lint_code for quality
```

### Refactoring Workflow
```
1. find_references to check impact
2. rename_symbol or extract_method (dry-run first)
3. Confirm changes
4. run_tests to verify
5. git_commit the refactoring
```

---

## Statistics

- **Total Tools**: 38
- **File Operations**: 10 (26%)
- **Git Operations**: 10 (26%)
- **Code Analysis**: 6 (16%)
- **Shell Execution**: 3 (8%)
- **Search**: 4 (11%)
- **Testing**: 3 (8%)
- **Refactoring**: 2 (5%)
- **Documentation**: 2 (5%)

---

## Performance Guidelines

### Fast Operations (< 100ms)
- File read/write (small files)
- Git status
- List directory
- File info

### Medium Operations (100ms - 1s)
- Code parsing
- Git log
- Search operations
- Complexity analysis

### Slow Operations (> 1s)
- Find references (large projects)
- Lint code (full project)
- Detect duplication (large codebase)
- Build project
- Run tests

---

Generated: December 17, 2025  
Version: Phase 3 Complete  
Total Lines of Tool Code: ~15,000+
