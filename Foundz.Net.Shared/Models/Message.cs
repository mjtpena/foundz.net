namespace Foundz.Net.Shared.Models;

/// <summary>
/// Represents a message in a conversation.
/// </summary>
public record Message
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required MessageRole Role { get; init; }
    public required string Content { get; init; }
    public int? TokenCount { get; init; }
    public DateTime? Timestamp { get; init; } = DateTime.UtcNow;
    public Guid? ParentMessageId { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public List<ToolCall>? ToolCalls { get; init; }
    
    // For tool result messages
    public string? ToolCallId { get; init; }
    public string? ToolName { get; init; }
}

/// <summary>
/// Represents the role of a message sender.
/// </summary>
public enum MessageRole
{
    System,
    User,
    Assistant,
    Tool
}
