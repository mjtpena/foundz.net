using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Foundz.Net.Core.AI;

/// <summary>
/// Tracks AI API costs across requests for budgeting and reporting.
/// </summary>
public class CostTracker
{
    private readonly ILogger<CostTracker> _logger;
    private readonly ConcurrentDictionary<string, ModelPricing> _modelPricing;
    private readonly ConcurrentDictionary<string, List<CostEntry>> _sessionCosts;
    private decimal _totalCost;
    private readonly object _lock = new();

    public CostTracker(ILogger<CostTracker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelPricing = new ConcurrentDictionary<string, ModelPricing>();
        _sessionCosts = new ConcurrentDictionary<string, List<CostEntry>>();
        _totalCost = 0m;

        // Initialize default pricing (prices per 1,000 tokens)
        InitializeDefaultPricing();
    }

    /// <summary>
    /// Records the cost of an AI request.
    /// </summary>
    public void RecordRequest(
        string sessionId,
        string modelName,
        int promptTokens,
        int completionTokens,
        DateTime timestamp)
    {
        if (!_modelPricing.TryGetValue(modelName, out var pricing))
        {
            _logger.LogWarning("Model pricing not found for {Model}, using default rates", modelName);
            pricing = new ModelPricing
            {
                ModelName = modelName,
                InputCostPer1kTokens = 1.0m,  // Default fallback
                OutputCostPer1kTokens = 3.0m
            };
        }

        var inputCost = (promptTokens / 1000.0m) * pricing.InputCostPer1kTokens;
        var outputCost = (completionTokens / 1000.0m) * pricing.OutputCostPer1kTokens;
        var totalRequestCost = inputCost + outputCost;

        var entry = new CostEntry
        {
            SessionId = sessionId,
            ModelName = modelName,
            Timestamp = timestamp,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            InputCost = inputCost,
            OutputCost = outputCost,
            TotalCost = totalRequestCost
        };

        // Add to session costs
        _sessionCosts.AddOrUpdate(
            sessionId,
            _ => new List<CostEntry> { entry },
            (_, list) => { list.Add(entry); return list; }
        );

        // Update total cost
        lock (_lock)
        {
            _totalCost += totalRequestCost;
        }

        _logger.LogDebug(
            "Recorded cost: {Model} - {PromptTokens} prompt + {CompletionTokens} completion = ${Cost:F4}",
            modelName, promptTokens, completionTokens, totalRequestCost);
    }

    /// <summary>
    /// Gets the total cost for a specific session.
    /// </summary>
    public decimal GetSessionCost(string sessionId)
    {
        if (_sessionCosts.TryGetValue(sessionId, out var costs))
        {
            return costs.Sum(c => c.TotalCost);
        }

        return 0m;
    }

    /// <summary>
    /// Gets detailed cost breakdown for a session.
    /// </summary>
    public SessionCostSummary GetSessionSummary(string sessionId)
    {
        if (!_sessionCosts.TryGetValue(sessionId, out var costs) || !costs.Any())
        {
            return new SessionCostSummary
            {
                SessionId = sessionId,
                TotalCost = 0m,
                RequestCount = 0,
                TotalPromptTokens = 0,
                TotalCompletionTokens = 0,
                ModelBreakdown = new Dictionary<string, ModelCostBreakdown>()
            };
        }

        var breakdown = costs
            .GroupBy(c => c.ModelName)
            .ToDictionary(
                g => g.Key,
                g => new ModelCostBreakdown
                {
                    ModelName = g.Key,
                    RequestCount = g.Count(),
                    TotalPromptTokens = g.Sum(c => c.PromptTokens),
                    TotalCompletionTokens = g.Sum(c => c.CompletionTokens),
                    TotalCost = g.Sum(c => c.TotalCost)
                }
            );

        return new SessionCostSummary
        {
            SessionId = sessionId,
            TotalCost = costs.Sum(c => c.TotalCost),
            RequestCount = costs.Count,
            TotalPromptTokens = costs.Sum(c => c.PromptTokens),
            TotalCompletionTokens = costs.Sum(c => c.CompletionTokens),
            ModelBreakdown = breakdown,
            FirstRequest = costs.Min(c => c.Timestamp),
            LastRequest = costs.Max(c => c.Timestamp)
        };
    }

    /// <summary>
    /// Gets the total cost across all sessions.
    /// </summary>
    public decimal GetTotalCost()
    {
        lock (_lock)
        {
            return _totalCost;
        }
    }

    /// <summary>
    /// Gets costs for a specific time period.
    /// </summary>
    public decimal GetCostForPeriod(DateTime start, DateTime end)
    {
        return _sessionCosts.Values
            .SelectMany(costs => costs)
            .Where(c => c.Timestamp >= start && c.Timestamp <= end)
            .Sum(c => c.TotalCost);
    }

    /// <summary>
    /// Gets total usage statistics.
    /// </summary>
    public UsageStatistics GetUsageStatistics()
    {
        var allCosts = _sessionCosts.Values.SelectMany(costs => costs).ToList();

        if (!allCosts.Any())
        {
            return new UsageStatistics
            {
                TotalCost = 0m,
                TotalRequests = 0,
                TotalPromptTokens = 0,
                TotalCompletionTokens = 0,
                ModelBreakdown = new Dictionary<string, ModelCostBreakdown>()
            };
        }

        var breakdown = allCosts
            .GroupBy(c => c.ModelName)
            .ToDictionary(
                g => g.Key,
                g => new ModelCostBreakdown
                {
                    ModelName = g.Key,
                    RequestCount = g.Count(),
                    TotalPromptTokens = g.Sum(c => c.PromptTokens),
                    TotalCompletionTokens = g.Sum(c => c.CompletionTokens),
                    TotalCost = g.Sum(c => c.TotalCost)
                }
            );

        return new UsageStatistics
        {
            TotalCost = GetTotalCost(),
            TotalRequests = allCosts.Count,
            TotalPromptTokens = allCosts.Sum(c => c.PromptTokens),
            TotalCompletionTokens = allCosts.Sum(c => c.CompletionTokens),
            ModelBreakdown = breakdown,
            SessionCount = _sessionCosts.Count,
            FirstRequest = allCosts.Min(c => c.Timestamp),
            LastRequest = allCosts.Max(c => c.Timestamp)
        };
    }

    /// <summary>
    /// Registers custom pricing for a model.
    /// </summary>
    public void RegisterModelPricing(string modelName, decimal inputCostPer1k, decimal outputCostPer1k)
    {
        var pricing = new ModelPricing
        {
            ModelName = modelName,
            InputCostPer1kTokens = inputCostPer1k,
            OutputCostPer1kTokens = outputCostPer1k
        };

        _modelPricing.AddOrUpdate(modelName, pricing, (_, _) => pricing);
        _logger.LogInformation("Registered pricing for {Model}: ${Input}/1K input, ${Output}/1K output",
            modelName, inputCostPer1k, outputCostPer1k);
    }

    /// <summary>
    /// Exports cost data to JSON.
    /// </summary>
    public string ExportToJson()
    {
        var data = new
        {
            TotalCost = GetTotalCost(),
            Sessions = _sessionCosts.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(c => new
                {
                    c.ModelName,
                    c.Timestamp,
                    c.PromptTokens,
                    c.CompletionTokens,
                    c.TotalCost
                })
            )
        };

        return System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Clears all cost data for a session.
    /// </summary>
    public void ClearSession(string sessionId)
    {
        if (_sessionCosts.TryRemove(sessionId, out var costs))
        {
            lock (_lock)
            {
                _totalCost -= costs.Sum(c => c.TotalCost);
            }

            _logger.LogInformation("Cleared cost data for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Resets all cost tracking data.
    /// </summary>
    public void Reset()
    {
        _sessionCosts.Clear();
        lock (_lock)
        {
            _totalCost = 0m;
        }

        _logger.LogWarning("All cost tracking data has been reset");
    }

    // Initialize default pricing for common models
    private void InitializeDefaultPricing()
    {
        // Claude models (Anthropic pricing as of Dec 2024)
        RegisterModelPricing("claude-3-5-sonnet-20241022", 3.0m, 15.0m);
        RegisterModelPricing("claude-3-opus-20240229", 15.0m, 75.0m);
        RegisterModelPricing("claude-3-sonnet-20240229", 3.0m, 15.0m);
        RegisterModelPricing("claude-3-haiku-20240307", 0.25m, 1.25m);

        // GPT-4 models (OpenAI pricing as of Dec 2024)
        RegisterModelPricing("gpt-4", 30.0m, 60.0m);
        RegisterModelPricing("gpt-4-turbo", 10.0m, 30.0m);
        RegisterModelPricing("gpt-4o", 5.0m, 15.0m);
        RegisterModelPricing("gpt-4o-mini", 0.15m, 0.60m);
        RegisterModelPricing("gpt-3.5-turbo", 0.50m, 1.50m);

        // Mistral models
        RegisterModelPricing("mistral-large", 8.0m, 24.0m);
        RegisterModelPricing("mistral-medium", 2.7m, 8.1m);
        RegisterModelPricing("mistral-small", 1.0m, 3.0m);

        // Cohere models
        RegisterModelPricing("command-r-plus", 3.0m, 15.0m);
        RegisterModelPricing("command-r", 0.50m, 1.50m);

        // Meta Llama (typical hosting costs)
        RegisterModelPricing("llama-3.1-405b", 5.0m, 15.0m);
        RegisterModelPricing("llama-3.1-70b", 0.90m, 0.90m);
        RegisterModelPricing("llama-3.1-8b", 0.20m, 0.20m);
    }
}

/// <summary>
/// Pricing information for a model.
/// </summary>
public record ModelPricing
{
    public required string ModelName { get; init; }
    public required decimal InputCostPer1kTokens { get; init; }
    public required decimal OutputCostPer1kTokens { get; init; }
}

/// <summary>
/// Single cost entry for a request.
/// </summary>
public record CostEntry
{
    public required string SessionId { get; init; }
    public required string ModelName { get; init; }
    public required DateTime Timestamp { get; init; }
    public required int PromptTokens { get; init; }
    public required int CompletionTokens { get; init; }
    public required decimal InputCost { get; init; }
    public required decimal OutputCost { get; init; }
    public required decimal TotalCost { get; init; }
}

/// <summary>
/// Cost summary for a session.
/// </summary>
public record SessionCostSummary
{
    public required string SessionId { get; init; }
    public required decimal TotalCost { get; init; }
    public required int RequestCount { get; init; }
    public required int TotalPromptTokens { get; init; }
    public required int TotalCompletionTokens { get; init; }
    public required Dictionary<string, ModelCostBreakdown> ModelBreakdown { get; init; }
    public DateTime? FirstRequest { get; init; }
    public DateTime? LastRequest { get; init; }
}

/// <summary>
/// Cost breakdown per model.
/// </summary>
public record ModelCostBreakdown
{
    public required string ModelName { get; init; }
    public required int RequestCount { get; init; }
    public required int TotalPromptTokens { get; init; }
    public required int TotalCompletionTokens { get; init; }
    public required decimal TotalCost { get; init; }
}

/// <summary>
/// Overall usage statistics.
/// </summary>
public record UsageStatistics
{
    public required decimal TotalCost { get; init; }
    public required int TotalRequests { get; init; }
    public required int TotalPromptTokens { get; init; }
    public required int TotalCompletionTokens { get; init; }
    public required Dictionary<string, ModelCostBreakdown> ModelBreakdown { get; init; }
    public int SessionCount { get; init; }
    public DateTime? FirstRequest { get; init; }
    public DateTime? LastRequest { get; init; }
}
