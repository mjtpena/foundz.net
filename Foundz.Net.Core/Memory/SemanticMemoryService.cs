using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;

#pragma warning disable SKEXP0001 // Suppress experimental API warning
using Microsoft.SemanticKernel.Memory;
#pragma warning restore SKEXP0001

namespace Foundz.Net.Core.Memory;

/// <summary>
/// Manages semantic memory for conversations using Semantic Kernel.
/// Provides volatile, short-term, long-term, and project memory types.
/// </summary>
#pragma warning disable SKEXP0001 // Experimental API
public class SemanticMemoryService
{
    private readonly ILogger<SemanticMemoryService> _logger;
    private readonly ISemanticTextMemory? _semanticMemory; // Optional - requires embeddings
    
    // In-memory stores for different memory types
    private readonly Dictionary<string, List<MemoryEntry>> _volatileMemory; // Current conversation
    private readonly Dictionary<string, List<MemoryEntry>> _shortTermMemory; // Session facts
    private readonly Dictionary<string, List<MemoryEntry>> _longTermMemory; // Cross-session
    private readonly Dictionary<string, Dictionary<string, string>> _projectMemory; // Project metadata

    public SemanticMemoryService(
        ILogger<SemanticMemoryService> logger,
        ISemanticTextMemory? semanticMemory = null)
    {
        _logger = logger;
        _semanticMemory = semanticMemory;
        _volatileMemory = new Dictionary<string, List<MemoryEntry>>();
        _shortTermMemory = new Dictionary<string, List<MemoryEntry>>();
        _longTermMemory = new Dictionary<string, List<MemoryEntry>>();
        _projectMemory = new Dictionary<string, Dictionary<string, string>>();
    }

    #region Volatile Memory (Current Conversation)

    /// <summary>
    /// Add entry to volatile memory (cleared after conversation)
    /// </summary>
    public void AddVolatileMemory(string sessionId, string key, string value, string? category = null)
    {
        if (!_volatileMemory.ContainsKey(sessionId))
        {
            _volatileMemory[sessionId] = new List<MemoryEntry>();
        }

        _volatileMemory[sessionId].Add(new MemoryEntry
        {
            Key = key,
            Value = value,
            Category = category,
            Timestamp = DateTime.UtcNow,
            Type = MemoryType.Volatile
        });

        _logger.LogTrace("Added volatile memory: {Key} for session {SessionId}", key, sessionId);
    }

    /// <summary>
    /// Get all volatile memory for a session
    /// </summary>
    public List<MemoryEntry> GetVolatileMemory(string sessionId)
    {
        return _volatileMemory.GetValueOrDefault(sessionId, new List<MemoryEntry>());
    }

    /// <summary>
    /// Clear volatile memory for a session
    /// </summary>
    public void ClearVolatileMemory(string sessionId)
    {
        _volatileMemory.Remove(sessionId);
        _logger.LogDebug("Cleared volatile memory for session: {SessionId}", sessionId);
    }

    #endregion

    #region Short-Term Memory (Session Facts)

    /// <summary>
    /// Add entry to short-term memory (persists for session duration)
    /// </summary>
    public void AddShortTermMemory(string sessionId, string key, string value, string? category = null)
    {
        if (!_shortTermMemory.ContainsKey(sessionId))
        {
            _shortTermMemory[sessionId] = new List<MemoryEntry>();
        }

        // Remove existing entry with same key
        _shortTermMemory[sessionId].RemoveAll(e => e.Key == key);

        _shortTermMemory[sessionId].Add(new MemoryEntry
        {
            Key = key,
            Value = value,
            Category = category,
            Timestamp = DateTime.UtcNow,
            Type = MemoryType.ShortTerm
        });

        _logger.LogTrace("Added short-term memory: {Key} for session {SessionId}", key, sessionId);
    }

    /// <summary>
    /// Get short-term memory for a session
    /// </summary>
    public List<MemoryEntry> GetShortTermMemory(string sessionId, string? category = null)
    {
        var memories = _shortTermMemory.GetValueOrDefault(sessionId, new List<MemoryEntry>());
        
        if (category != null)
        {
            memories = memories.Where(m => m.Category == category).ToList();
        }

        return memories;
    }

    /// <summary>
    /// Search short-term memory
    /// </summary>
    public List<MemoryEntry> SearchShortTermMemory(string sessionId, string query)
    {
        var memories = GetShortTermMemory(sessionId);
        return memories.Where(m => 
            m.Key.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            m.Value.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Timestamp)
            .ToList();
    }

    #endregion

    #region Long-Term Memory (Cross-Session)

    /// <summary>
    /// Add entry to long-term memory (persists across sessions)
    /// </summary>
    public async Task AddLongTermMemoryAsync(
        string userId, 
        string key, 
        string value, 
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        if (!_longTermMemory.ContainsKey(userId))
        {
            _longTermMemory[userId] = new List<MemoryEntry>();
        }

        var entry = new MemoryEntry
        {
            Key = key,
            Value = value,
            Category = category,
            Timestamp = DateTime.UtcNow,
            Type = MemoryType.LongTerm
        };

        _longTermMemory[userId].Add(entry);

        // If semantic memory is available, store there too
        if (_semanticMemory != null)
        {
            try
            {
                await _semanticMemory.SaveInformationAsync(
                    collection: $"user_{userId}",
                    text: value,
                    id: key,
                    description: category,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save to semantic memory: {Key}", key);
            }
        }

        _logger.LogDebug("Added long-term memory: {Key} for user {UserId}", key, userId);
    }

    /// <summary>
    /// Get long-term memory for a user
    /// </summary>
    public List<MemoryEntry> GetLongTermMemory(string userId, string? category = null)
    {
        var memories = _longTermMemory.GetValueOrDefault(userId, new List<MemoryEntry>());
        
        if (category != null)
        {
            memories = memories.Where(m => m.Category == category).ToList();
        }

        return memories.OrderByDescending(m => m.Timestamp).ToList();
    }

    /// <summary>
    /// Search long-term memory with semantic search (if available)
    /// </summary>
    public async Task<List<MemoryEntry>> SearchLongTermMemoryAsync(
        string userId, 
        string query,
        int limit = 10,
        double minRelevance = 0.7,
        CancellationToken cancellationToken = default)
    {
        var results = new List<MemoryEntry>();

        // Try semantic search first
        if (_semanticMemory != null)
        {
            try
            {
                var semanticResults = _semanticMemory.SearchAsync(
                    collection: $"user_{userId}",
                    query: query,
                    limit: limit,
                    minRelevanceScore: minRelevance,
                    cancellationToken: cancellationToken);

                await foreach (var result in semanticResults.ConfigureAwait(false))
                {
                    results.Add(new MemoryEntry
                    {
                        Key = result.Metadata.Id,
                        Value = result.Metadata.Text,
                        Category = result.Metadata.Description,
                        Timestamp = DateTime.UtcNow,
                        Type = MemoryType.LongTerm,
                        RelevanceScore = result.Relevance
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Semantic search failed, falling back to keyword search");
            }
        }

        // Fallback to keyword search
        var memories = GetLongTermMemory(userId);
        return memories.Where(m => 
            m.Key.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            m.Value.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .ToList();
    }

    #endregion

    #region Project Memory (Project Metadata)

    /// <summary>
    /// Set project metadata
    /// </summary>
    public void SetProjectMetadata(string projectPath, string key, string value)
    {
        if (!_projectMemory.ContainsKey(projectPath))
        {
            _projectMemory[projectPath] = new Dictionary<string, string>();
        }

        _projectMemory[projectPath][key] = value;
        _logger.LogTrace("Set project metadata: {Key}={Value} for {Path}", key, value, projectPath);
    }

    /// <summary>
    /// Get project metadata
    /// </summary>
    public string? GetProjectMetadata(string projectPath, string key)
    {
        return _projectMemory.GetValueOrDefault(projectPath)?.GetValueOrDefault(key);
    }

    /// <summary>
    /// Get all project metadata
    /// </summary>
    public Dictionary<string, string> GetAllProjectMetadata(string projectPath)
    {
        return _projectMemory.GetValueOrDefault(projectPath, new Dictionary<string, string>());
    }

    #endregion

    #region Memory Retrieval & Context Building

    /// <summary>
    /// Build context from all memory types for a request
    /// </summary>
    public async Task<MemoryContext> BuildContextAsync(
        string sessionId,
        string userId,
        string projectPath,
        string query,
        CancellationToken cancellationToken = default)
    {
        var context = new MemoryContext
        {
            VolatileMemories = GetVolatileMemory(sessionId),
            ShortTermMemories = GetShortTermMemory(sessionId),
            LongTermMemories = await SearchLongTermMemoryAsync(userId, query, 5, 0.7, cancellationToken),
            ProjectMetadata = GetAllProjectMetadata(projectPath),
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogDebug(
            "Built memory context: {Volatile} volatile, {ShortTerm} short-term, {LongTerm} long-term",
            context.VolatileMemories.Count,
            context.ShortTermMemories.Count,
            context.LongTermMemories.Count);

        return context;
    }

    /// <summary>
    /// Format memory context as text for AI consumption
    /// </summary>
    public string FormatContextForAI(MemoryContext context)
    {
        var parts = new List<string>();

        if (context.ProjectMetadata.Any())
        {
            parts.Add("## Project Information");
            foreach (var kvp in context.ProjectMetadata)
            {
                parts.Add($"- {kvp.Key}: {kvp.Value}");
            }
        }

        if (context.ShortTermMemories.Any())
        {
            parts.Add("\n## Session Context");
            foreach (var memory in context.ShortTermMemories.Take(10))
            {
                parts.Add($"- {memory.Key}: {memory.Value}");
            }
        }

        if (context.LongTermMemories.Any())
        {
            parts.Add("\n## Relevant Background");
            foreach (var memory in context.LongTermMemories)
            {
                parts.Add($"- {memory.Value}");
            }
        }

        return string.Join("\n", parts);
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleanup old memories
    /// </summary>
    public void CleanupOldMemories(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var cleaned = 0;

        foreach (var sessionId in _volatileMemory.Keys.ToList())
        {
            var before = _volatileMemory[sessionId].Count;
            _volatileMemory[sessionId].RemoveAll(m => m.Timestamp < cutoff);
            cleaned += before - _volatileMemory[sessionId].Count;
            
            if (_volatileMemory[sessionId].Count == 0)
            {
                _volatileMemory.Remove(sessionId);
            }
        }

        foreach (var sessionId in _shortTermMemory.Keys.ToList())
        {
            var before = _shortTermMemory[sessionId].Count;
            _shortTermMemory[sessionId].RemoveAll(m => m.Timestamp < cutoff);
            cleaned += before - _shortTermMemory[sessionId].Count;
            
            if (_shortTermMemory[sessionId].Count == 0)
            {
                _shortTermMemory.Remove(sessionId);
            }
        }

        _logger.LogInformation("Cleaned up {Count} old memory entries", cleaned);
    }

    #endregion
}

/// <summary>
/// Memory entry
/// </summary>
public class MemoryEntry
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime Timestamp { get; set; }
    public MemoryType Type { get; set; }
    public double? RelevanceScore { get; set; }
}

/// <summary>
/// Memory type classification
/// </summary>
public enum MemoryType
{
    Volatile,      // Current conversation only
    ShortTerm,     // Session duration
    LongTerm,      // Cross-session
    Project        // Project-specific
}

/// <summary>
/// Complete memory context for a request
/// </summary>
public class MemoryContext
{
    public List<MemoryEntry> VolatileMemories { get; set; } = new();
    public List<MemoryEntry> ShortTermMemories { get; set; } = new();
    public List<MemoryEntry> LongTermMemories { get; set; } = new();
    public Dictionary<string, string> ProjectMetadata { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}
