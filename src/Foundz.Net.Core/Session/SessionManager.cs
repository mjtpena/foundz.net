using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.Session;

/// <summary>
/// Manages conversation sessions with persistence support.
/// </summary>
public class SessionManager
{
    private readonly ILogger<SessionManager> _logger;
    private readonly Dictionary<Guid, Foundz.Net.Shared.Models.Session> _sessions = new();

    public SessionManager(ILogger<SessionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new session.
    /// </summary>
    public Foundz.Net.Shared.Models.Session CreateSession(
        string? userId = null,
        string? projectPath = null,
        string? modelName = null)
    {
        var session = new Foundz.Net.Shared.Models.Session
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Messages = new List<Message>(),
            UserId = userId,
            ProjectPath = projectPath,
            ModelName = modelName ?? "claude-3-5-sonnet-20241022"
        };

        _sessions[session.Id] = session;
        
        _logger.LogInformation("Created new session {SessionId}", session.Id);
        
        return session;
    }

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    public Foundz.Net.Shared.Models.Session? GetSession(Guid sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    /// <summary>
    /// Updates a session.
    /// </summary>
    public void UpdateSession(Foundz.Net.Shared.Models.Session session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        session = session with { UpdatedAt = DateTime.UtcNow };
        _sessions[session.Id] = session;
        
        _logger.LogDebug("Updated session {SessionId}", session.Id);
    }

    /// <summary>
    /// Deletes a session.
    /// </summary>
    public bool DeleteSession(Guid sessionId)
    {
        if (_sessions.Remove(sessionId))
        {
            _logger.LogInformation("Deleted session {SessionId}", sessionId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Lists all sessions.
    /// </summary>
    public List<Foundz.Net.Shared.Models.Session> ListSessions(
        string? userId = null,
        int? limit = null)
    {
        var sessions = _sessions.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            sessions = sessions.Where(s => s.UserId == userId);
        }

        sessions = sessions.OrderByDescending(s => s.UpdatedAt);

        if (limit.HasValue && limit.Value > 0)
        {
            sessions = sessions.Take(limit.Value);
        }

        return sessions.ToList();
    }

    /// <summary>
    /// Adds a message to a session.
    /// </summary>
    public void AddMessage(Guid sessionId, Message message)
    {
        var session = GetSession(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Cannot add message to unknown session {SessionId}", sessionId);
            return;
        }

        var messages = new List<Message>(session.Messages) { message };
        var updatedSession = session with 
        { 
            Messages = messages,
            UpdatedAt = DateTime.UtcNow
        };

        _sessions[sessionId] = updatedSession;
        
        _logger.LogDebug("Added {Role} message to session {SessionId}", 
            message.Role, sessionId);
    }

    /// <summary>
    /// Clears all messages from a session.
    /// </summary>
    public void ClearMessages(Guid sessionId)
    {
        var session = GetSession(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Cannot clear messages from unknown session {SessionId}", sessionId);
            return;
        }

        var updatedSession = session with 
        { 
            Messages = new List<Message>(),
            UpdatedAt = DateTime.UtcNow
        };

        _sessions[sessionId] = updatedSession;
        
        _logger.LogInformation("Cleared messages from session {SessionId}", sessionId);
    }

    /// <summary>
    /// Archives old sessions.
    /// </summary>
    public int ArchiveOldSessions(TimeSpan olderThan)
    {
        var cutoffTime = DateTime.UtcNow - olderThan;
        var toArchive = _sessions.Values
            .Where(s => s.UpdatedAt < cutoffTime)
            .Select(s => s.Id)
            .ToList();

        foreach (var sessionId in toArchive)
        {
            _sessions.Remove(sessionId);
        }

        _logger.LogInformation("Archived {Count} old sessions", toArchive.Count);
        
        return toArchive.Count;
    }

    /// <summary>
    /// Exports a session to JSON.
    /// </summary>
    public string? ExportSession(Guid sessionId)
    {
        var session = GetSession(sessionId);
        if (session == null)
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Serialize(session, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export session {SessionId}", sessionId);
            return null;
        }
    }

    /// <summary>
    /// Imports a session from JSON.
    /// </summary>
    public Foundz.Net.Shared.Models.Session? ImportSession(string json)
    {
        try
        {
            var session = System.Text.Json.JsonSerializer.Deserialize<Foundz.Net.Shared.Models.Session>(json);
            if (session != null)
            {
                _sessions[session.Id] = session;
                _logger.LogInformation("Imported session {SessionId}", session.Id);
                return session;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import session");
            return null;
        }
    }

    /// <summary>
    /// Gets count of active sessions.
    /// </summary>
    public int Count => _sessions.Count;
}
