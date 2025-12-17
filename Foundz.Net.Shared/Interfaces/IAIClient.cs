using Foundz.Net.Shared.Models;

namespace Foundz.Net.Shared.Interfaces;

/// <summary>
/// Interface for AI client implementations.
/// </summary>
public interface IAIClient
{
    /// <summary>
    /// Sends a message to the AI model and returns the complete response.
    /// </summary>
    Task<AIResponse> SendMessageAsync(
        List<Message> messages, 
        List<ToolDefinition>? tools = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a message to the AI model and streams the response.
    /// </summary>
    IAsyncEnumerable<AIResponseChunk> StreamMessageAsync(
        List<Message> messages, 
        List<ToolDefinition>? tools = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lists available models.
    /// </summary>
    Task<List<string>> ListModelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the capabilities of a specific model.
    /// </summary>
    Task<ModelCapabilities> GetModelCapabilitiesAsync(string modelName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates the connection to the AI service.
    /// </summary>
    Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a complete AI response.
/// </summary>
public record AIResponse
{
    public required string Content { get; init; }
    public List<ToolCall>? ToolCalls { get; init; }
    public required TokenUsage TokenUsage { get; init; }
    public required string FinishReason { get; init; }
    public string? ModelUsed { get; init; }
}

/// <summary>
/// Represents a streaming chunk of AI response.
/// </summary>
public record AIResponseChunk
{
    public string? ContentDelta { get; init; }
    public List<ToolCall>? ToolCalls { get; init; }
    public TokenUsage? TokenUsage { get; init; }
    public string? FinishReason { get; init; }
}

/// <summary>
/// Represents token usage statistics.
/// </summary>
public record TokenUsage
{
    public required int PromptTokens { get; init; }
    public required int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}

/// <summary>
/// Represents a tool definition for the AI.
/// </summary>
public record ToolDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required Dictionary<string, object> Parameters { get; init; }
}
