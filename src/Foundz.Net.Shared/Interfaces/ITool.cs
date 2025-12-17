using Foundz.Net.Shared.Models;

namespace Foundz.Net.Shared.Interfaces;

/// <summary>
/// Represents a tool that can be used by the AI agent.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Unique identifier for the tool.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Human-readable description for the AI to understand the tool's purpose.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Category for organizing tools.
    /// </summary>
    ToolCategory Category { get; }
    
    /// <summary>
    /// JSON schema for the tool's parameters.
    /// </summary>
    string ParametersSchema { get; }
    
    /// <summary>
    /// Whether this tool requires user confirmation before execution.
    /// </summary>
    bool RequiresConfirmation { get; }
    
    /// <summary>
    /// Danger level of the tool.
    /// </summary>
    DangerLevel DangerLevel { get; }
    
    /// <summary>
    /// Validates the arguments for the tool.
    /// </summary>
    Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes the tool with the given arguments.
    /// </summary>
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets usage examples for the tool.
    /// </summary>
    IEnumerable<string> GetExamples();
}

/// <summary>
/// Tool categories for organization.
/// </summary>
public enum ToolCategory
{
    FileOperations,
    GitOperations,
    CodeAnalysis,
    ShellExecution,
    Search,
    Testing,
    Refactoring,
    Documentation
}

/// <summary>
/// Danger levels for tools.
/// </summary>
public enum DangerLevel
{
    Safe,
    Warning,
    Danger
}
