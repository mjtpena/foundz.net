namespace Foundz.Net.Shared.Models;

/// <summary>
/// Represents a tool call request from the AI model.
/// </summary>
public record ToolCall
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required Dictionary<string, object> Arguments { get; init; }
}

/// <summary>
/// Represents the result of a tool execution.
/// </summary>
public record ToolResult
{
    public required string ToolCallId { get; init; }
    public required string ToolName { get; init; }
    public required bool Success { get; init; }
    public string? Output { get; init; }
    public string? Error { get; init; }
    public int ExecutionTimeMs { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}
