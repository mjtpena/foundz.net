using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.Orchestration;

/// <summary>
/// Manages conversation history with support for branching and search.
/// </summary>
public class ConversationHistory
{
    private readonly List<Message> _messages = new();
    private readonly ILogger<ConversationHistory> _logger;

    public ConversationHistory(ILogger<ConversationHistory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds a message to the history.
    /// </summary>
    public void AddMessage(Message message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        _messages.Add(message);
        _logger.LogDebug("Added {Role} message to history (total: {Count})", 
            message.Role, _messages.Count);
    }

    /// <summary>
    /// Adds multiple messages to the history.
    /// </summary>
    public void AddMessages(IEnumerable<Message> messages)
    {
        if (messages == null)
            throw new ArgumentNullException(nameof(messages));

        foreach (var message in messages)
        {
            AddMessage(message);
        }
    }

    /// <summary>
    /// Gets all messages in the history.
    /// </summary>
    public List<Message> GetAll()
    {
        return _messages.ToList();
    }

    /// <summary>
    /// Gets the last N messages.
    /// </summary>
    public List<Message> GetRecent(int count)
    {
        if (count <= 0)
            return new List<Message>();

        return _messages.TakeLast(count).ToList();
    }

    /// <summary>
    /// Gets messages by role.
    /// </summary>
    public List<Message> GetByRole(MessageRole role)
    {
        return _messages.Where(m => m.Role == role).ToList();
    }

    /// <summary>
    /// Searches messages by content.
    /// </summary>
    public List<Message> Search(string query, bool caseSensitive = false)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<Message>();

        var comparison = caseSensitive 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;

        return _messages
            .Where(m => m.Content != null && m.Content.Contains(query, comparison))
            .ToList();
    }

    /// <summary>
    /// Gets the count of messages in the history.
    /// </summary>
    public int Count => _messages.Count;

    /// <summary>
    /// Clears all messages from the history.
    /// </summary>
    public void Clear()
    {
        _messages.Clear();
        _logger.LogInformation("Cleared conversation history");
    }

    /// <summary>
    /// Exports the conversation history to JSON.
    /// </summary>
    public string ExportToJson()
    {
        try
        {
            return System.Text.Json.JsonSerializer.Serialize(_messages, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export conversation history");
            throw;
        }
    }

    /// <summary>
    /// Imports conversation history from JSON.
    /// </summary>
    public void ImportFromJson(string json)
    {
        try
        {
            var messages = System.Text.Json.JsonSerializer.Deserialize<List<Message>>(json);
            if (messages != null)
            {
                _messages.Clear();
                _messages.AddRange(messages);
                _logger.LogInformation("Imported {Count} messages from JSON", messages.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import conversation history");
            throw;
        }
    }

    /// <summary>
    /// Gets a summary of the conversation.
    /// </summary>
    public ConversationSummary GetSummary()
    {
        return new ConversationSummary
        {
            TotalMessages = _messages.Count,
            UserMessages = _messages.Count(m => m.Role == MessageRole.User),
            AssistantMessages = _messages.Count(m => m.Role == MessageRole.Assistant),
            ToolMessages = _messages.Count(m => m.Role == MessageRole.Tool),
            SystemMessages = _messages.Count(m => m.Role == MessageRole.System),
            FirstMessageTime = _messages.FirstOrDefault()?.Timestamp,
            LastMessageTime = _messages.LastOrDefault()?.Timestamp
        };
    }

    /// <summary>
    /// Truncates history to keep only recent messages.
    /// </summary>
    public void Truncate(int keepCount)
    {
        if (keepCount <= 0 || _messages.Count <= keepCount)
            return;

        var toRemove = _messages.Count - keepCount;
        _messages.RemoveRange(0, toRemove);
        
        _logger.LogInformation("Truncated conversation history: removed {Count} old messages", toRemove);
    }

    /// <summary>
    /// Gets messages in a time range.
    /// </summary>
    public List<Message> GetByTimeRange(DateTime startTime, DateTime endTime)
    {
        return _messages
            .Where(m => m.Timestamp.HasValue && 
                       m.Timestamp >= startTime && 
                       m.Timestamp <= endTime)
            .ToList();
    }
}

/// <summary>
/// Represents a summary of the conversation.
/// </summary>
public record ConversationSummary
{
    public required int TotalMessages { get; init; }
    public required int UserMessages { get; init; }
    public required int AssistantMessages { get; init; }
    public required int ToolMessages { get; init; }
    public required int SystemMessages { get; init; }
    public DateTime? FirstMessageTime { get; init; }
    public DateTime? LastMessageTime { get; init; }
}
