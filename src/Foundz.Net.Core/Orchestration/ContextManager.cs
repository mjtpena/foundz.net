using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.Orchestration;

/// <summary>
/// Manages conversation context with sliding window and token budget.
/// </summary>
public class ContextManager
{
    private readonly ILogger<ContextManager> _logger;
    private readonly int _maxMessages;
    private readonly int _maxTokens;
    private readonly int _tokenReserve;

    public ContextManager(
        ILogger<ContextManager> logger,
        int maxMessages = 20,
        int maxTokens = 150000,
        int tokenReserve = 20000)
    {
        _logger = logger;
        _maxMessages = maxMessages;
        _maxTokens = maxTokens;
        _tokenReserve = tokenReserve;
    }

    /// <summary>
    /// Builds context from conversation history with sliding window.
    /// </summary>
    public List<Message> BuildContext(
        List<Message> fullHistory,
        string? systemPrompt = null)
    {
        var context = new List<Message>();

        // Add system message if provided
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            context.Add(new Message
            {
                Role = MessageRole.System,
                Content = systemPrompt
            });
        }

        // Apply sliding window
        var recentMessages = ApplySlidingWindow(fullHistory);
        context.AddRange(recentMessages);

        // Apply token budget
        context = ApplyTokenBudget(context);

        _logger.LogDebug(
            "Built context with {Count} messages (max: {Max})",
            context.Count, _maxMessages);

        return context;
    }

    /// <summary>
    /// Applies sliding window to keep only recent messages.
    /// </summary>
    private List<Message> ApplySlidingWindow(List<Message> messages)
    {
        if (messages.Count <= _maxMessages)
            return messages.ToList();

        _logger.LogDebug(
            "Applying sliding window: {Total} messages -> {Max} messages",
            messages.Count, _maxMessages);

        // Keep the most recent messages
        return messages.TakeLast(_maxMessages).ToList();
    }

    /// <summary>
    /// Applies token budget to ensure we don't exceed context limit.
    /// </summary>
    private List<Message> ApplyTokenBudget(List<Message> messages)
    {
        var totalTokens = EstimateTokens(messages);
        var budget = _maxTokens - _tokenReserve;

        if (totalTokens <= budget)
            return messages;

        _logger.LogWarning(
            "Context exceeds token budget ({Total} > {Budget}), truncating",
            totalTokens, budget);

        // Truncate from oldest (but keep system message if present)
        var result = new List<Message>();
        var systemMsg = messages.FirstOrDefault(m => m.Role == MessageRole.System);
        
        if (systemMsg != null)
        {
            result.Add(systemMsg);
            messages = messages.Skip(1).ToList();
        }

        var currentTokens = systemMsg != null ? EstimateTokens(systemMsg) : 0;

        // Add messages from newest to oldest until budget is reached
        for (int i = messages.Count - 1; i >= 0; i--)
        {
            var msgTokens = EstimateTokens(messages[i]);
            if (currentTokens + msgTokens > budget)
                break;

            result.Insert(systemMsg != null ? 1 : 0, messages[i]);
            currentTokens += msgTokens;
        }

        return result;
    }

    /// <summary>
    /// Estimates token count for messages.
    /// </summary>
    public int EstimateTokens(List<Message> messages)
    {
        return messages.Sum(EstimateTokens);
    }

    /// <summary>
    /// Estimates token count for a single message.
    /// </summary>
    public int EstimateTokens(Message message)
    {
        if (message == null)
            return 0;

        // Rough estimation: ~4 characters per token
        var charCount = message.Content?.Length ?? 0;
        var tokens = (int)Math.Ceiling(charCount / 4.0);

        // Add overhead for message structure
        tokens += 10;

        return tokens;
    }

    /// <summary>
    /// Adds relevant files to context.
    /// </summary>
    public void AddRelevantFiles(
        List<Message> context,
        List<string> filePaths,
        Func<string, string?> fileReader)
    {
        foreach (var filePath in filePaths)
        {
            try
            {
                var content = fileReader(filePath);
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                var fileMessage = new Message
                {
                    Role = MessageRole.System,
                    Content = $"File: {filePath}\n\n```\n{content}\n```"
                };

                // Check if adding this file would exceed token budget
                if (EstimateTokens(context) + EstimateTokens(fileMessage) < _maxTokens - _tokenReserve)
                {
                    context.Add(fileMessage);
                    _logger.LogDebug("Added file to context: {FilePath}", filePath);
                }
                else
                {
                    _logger.LogWarning("Skipping file due to token budget: {FilePath}", filePath);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add file to context: {FilePath}", filePath);
            }
        }
    }

    /// <summary>
    /// Adds project metadata to context.
    /// </summary>
    public void AddProjectMetadata(
        List<Message> context,
        Dictionary<string, string> metadata)
    {
        if (metadata == null || metadata.Count == 0)
            return;

        var metadataContent = "Project Context:\n";
        foreach (var kvp in metadata)
        {
            metadataContent += $"- {kvp.Key}: {kvp.Value}\n";
        }

        context.Insert(0, new Message
        {
            Role = MessageRole.System,
            Content = metadataContent
        });

        _logger.LogDebug("Added project metadata to context");
    }

    /// <summary>
    /// Summarizes old messages to save tokens.
    /// </summary>
    public async Task<Message> SummarizeMessagesAsync(
        List<Message> messages,
        Func<string, Task<string>> summarizerFunc)
    {
        if (messages.Count == 0)
            return new Message { Role = MessageRole.System, Content = "No messages to summarize" };

        try
        {
            var combinedContent = string.Join("\n\n", messages.Select(m => $"{m.Role}: {m.Content}"));
            var summary = await summarizerFunc($"Summarize this conversation:\n\n{combinedContent}");

            return new Message
            {
                Role = MessageRole.System,
                Content = $"Summary of previous conversation:\n{summary}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to summarize messages");
            return new Message
            {
                Role = MessageRole.System,
                Content = "Failed to generate summary of previous conversation"
            };
        }
    }

    /// <summary>
    /// Gets remaining token budget.
    /// </summary>
    public int GetRemainingBudget(List<Message> context)
    {
        var used = EstimateTokens(context);
        var available = _maxTokens - _tokenReserve;
        return Math.Max(0, available - used);
    }

    /// <summary>
    /// Checks if context is near token limit.
    /// </summary>
    public bool IsNearTokenLimit(List<Message> context, double threshold = 0.8)
    {
        var used = EstimateTokens(context);
        var available = _maxTokens - _tokenReserve;
        return used >= (available * threshold);
    }
}
