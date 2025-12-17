using Foundz.Net.Shared.Models;

namespace Foundz.Net.Shared.Interfaces;

/// <summary>
/// Interface for the agent orchestrator that manages the agentic loop.
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Processes a user message through the agentic loop.
    /// </summary>
    Task<AgentResponse> ProcessMessageAsync(
        string userMessage,
        Session session,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes a user message through the agentic loop with streaming.
    /// </summary>
    IAsyncEnumerable<AgentEvent> ProcessMessageStreamAsync(
        string userMessage,
        Session session,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the response from the agent.
/// </summary>
public record AgentResponse
{
    public required string Content { get; init; }
    public required List<ToolResult> ToolResults { get; init; }
    public required TokenUsage TokenUsage { get; init; }
    public required int IterationCount { get; init; }
    public bool HitMaxIterations { get; init; }
}

/// <summary>
/// Represents an event during agent processing.
/// </summary>
public record AgentEvent
{
    public required AgentEventType Type { get; init; }
    public string? Content { get; init; }
    public ToolCall? ToolCall { get; init; }
    public ToolResult? ToolResult { get; init; }
    public AgentState? State { get; init; }
}

/// <summary>
/// Types of agent events.
/// </summary>
public enum AgentEventType
{
    StateChanged,
    ContentDelta,
    ToolCallStarted,
    ToolCallCompleted,
    IterationCompleted,
    Error
}

/// <summary>
/// Agent states.
/// </summary>
public enum AgentState
{
    Idle,
    Thinking,
    ToolExecution,
    WaitingConfirmation,
    Streaming,
    Error,
    Completed
}
