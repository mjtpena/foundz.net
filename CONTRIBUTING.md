# Contributing to Foundz.Net

First off, thank you for considering contributing to Foundz.Net! It's people like you that make this project better for everyone.

## Code of Conduct

This project and everyone participating in it is governed by our commitment to fostering an open and welcoming environment. Please be respectful and constructive in all interactions.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the existing issues to avoid duplicates. When you create a bug report, include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples**
- **Describe the behavior you observed and what you expected**
- **Include screenshots if relevant**
- **Include your environment details** (.NET version, OS, etc.)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion:

- **Use a clear and descriptive title**
- **Provide a detailed description of the proposed feature**
- **Explain why this enhancement would be useful**
- **List any alternatives you've considered**

### Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Make your changes** following our code style guidelines
3. **Add tests** if you've added code that should be tested
4. **Ensure the test suite passes** (`dotnet test`)
5. **Update documentation** if needed
6. **Write a clear commit message** describing your changes

## Development Setup

```bash
# Clone your fork
git clone https://github.com/your-username/foundz.net.git
cd foundz.net

# Add upstream remote
git remote add upstream https://github.com/original-owner/foundz.net.git

# Install dependencies
dotnet restore

# Build the project
dotnet build

# Run tests
dotnet test
```

## Code Style Guidelines

### C# Conventions

- Follow [Microsoft's C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `async`/`await` for all I/O operations
- Add XML documentation comments for public APIs
- Use nullable reference types
- Keep methods focused and small (SRP)

### Example

```csharp
/// <summary>
/// Processes a user message through the agent orchestrator.
/// </summary>
/// <param name="message">The user's input message.</param>
/// <param name="session">The current session.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The agent's response.</returns>
public async Task<AgentResponse> ProcessMessageAsync(
    string message,
    Session session,
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

### Naming Conventions

- **Classes**: PascalCase (`AgentOrchestrator`)
- **Interfaces**: IPascalCase (`IToolRegistry`)
- **Methods**: PascalCase (`ProcessMessageAsync`)
- **Private fields**: _camelCase (`_logger`)
- **Parameters**: camelCase (`userMessage`)
- **Constants**: PascalCase (`MaxIterations`)

### File Organization

- One class per file
- File name matches class name
- Group related classes in same directory
- Keep files under 500 lines when possible

## Project Structure

```
Foundz.Net.X/
├── Models/           # Data models
├── Services/         # Business logic services
├── Interfaces/       # Interface definitions
├── Extensions/       # Extension methods
└── Utilities/        # Helper utilities
```

## Testing Guidelines

### Unit Tests

- Test one thing per test method
- Use descriptive test names
- Follow AAA pattern (Arrange, Act, Assert)
- Use FluentAssertions for assertions
- Mock external dependencies

```csharp
[Fact]
public void ToolRegistry_RegisterTool_ShouldAddToolToRegistry()
{
    // Arrange
    var registry = new ToolRegistry(logger);
    var tool = new MockTool();

    // Act
    registry.RegisterTool(tool);

    // Assert
    registry.GetTool("mock-tool").Should().NotBeNull();
}
```

### Integration Tests

- Test component interactions
- Use real implementations when possible
- Clean up resources after tests
- Mark with `[Category("Integration")]`

## Commit Message Guidelines

Write clear, concise commit messages:

```
feat: add batch task processing to TaskCommand

- Implement batch execution with progress tracking
- Add stop-on-error option
- Include comprehensive batch summary display
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

## Adding New Tools

To add a new tool to Foundz.Net:

1. Create a new class implementing `ITool` in the appropriate category folder
2. Define the tool's name, description, and parameters
3. Implement the `ExecuteAsync` method
4. Add XML documentation
5. Register in `ToolRegistry` if not using auto-discovery
6. Add unit tests
7. Update TOOLS_REFERENCE.md

Example:

```csharp
public class MyNewTool : ITool
{
    public string Name => "my_new_tool";
    public string Description => "Does something amazing";
    public ToolCategory Category => ToolCategory.Utilities;
    public Dictionary<string, object> Parameters => new()
    {
        ["input"] = new { type = "string", description = "Input parameter" }
    };

    public async Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

## Documentation

- Update README.md for user-facing changes
- Update TOOLS_REFERENCE.md for new tools
- Add XML comments to all public APIs
- Include code examples in documentation
- Keep documentation in sync with code

## Questions?

Feel free to open an issue with the `question` label, or reach out in our discussions forum.

## Recognition

Contributors will be recognized in:
- The project README
- Release notes
- GitHub contributors page

Thank you for contributing to Foundz.Net! 🎉
