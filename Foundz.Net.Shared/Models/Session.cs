namespace Foundz.Net.Shared.Models;

/// <summary>
/// Represents a conversation session.
/// </summary>
public record Session
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string UserId { get; init; }
    public required string ProjectPath { get; init; }
    public required string ModelName { get; init; }
    public SessionStatus Status { get; init; } = SessionStatus.Active;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; init; }
    public List<Message> Messages { get; init; } = [];
}

/// <summary>
/// Represents the status of a session.
/// </summary>
public enum SessionStatus
{
    Active,
    Archived,
    Deleted
}
