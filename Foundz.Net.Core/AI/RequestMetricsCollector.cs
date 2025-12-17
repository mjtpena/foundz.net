using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Foundz.Net.Core.AI;

/// <summary>
/// Collects and aggregates metrics for AI API requests.
/// </summary>
public class RequestMetricsCollector
{
    private readonly ILogger<RequestMetricsCollector> _logger;
    private readonly ConcurrentQueue<RequestMetric> _metrics;
    private readonly ConcurrentDictionary<string, ModelMetrics> _modelMetrics;
    private readonly int _maxStoredMetrics;

    public RequestMetricsCollector(
        ILogger<RequestMetricsCollector> logger,
        int maxStoredMetrics = 1000)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = new ConcurrentQueue<RequestMetric>();
        _modelMetrics = new ConcurrentDictionary<string, ModelMetrics>();
        _maxStoredMetrics = maxStoredMetrics;
    }

    /// <summary>
    /// Starts tracking a request.
    /// </summary>
    public RequestTracker StartRequest(string modelName, string sessionId)
    {
        return new RequestTracker(this, modelName, sessionId);
    }

    /// <summary>
    /// Records a completed request.
    /// </summary>
    internal void RecordRequest(RequestMetric metric)
    {
        _metrics.Enqueue(metric);

        // Update model-specific metrics
        _modelMetrics.AddOrUpdate(
            metric.ModelName,
            _ => new ModelMetrics
            {
                ModelName = metric.ModelName,
                TotalRequests = 1,
                SuccessfulRequests = metric.Success ? 1 : 0,
                FailedRequests = metric.Success ? 0 : 1,
                TotalDurationMs = metric.DurationMs,
                TotalPromptTokens = metric.PromptTokens ?? 0,
                TotalCompletionTokens = metric.CompletionTokens ?? 0,
                FirstRequest = metric.Timestamp,
                LastRequest = metric.Timestamp
            },
            (_, existing) => new ModelMetrics
            {
                ModelName = existing.ModelName,
                TotalRequests = existing.TotalRequests + 1,
                SuccessfulRequests = existing.SuccessfulRequests + (metric.Success ? 1 : 0),
                FailedRequests = existing.FailedRequests + (metric.Success ? 0 : 1),
                TotalDurationMs = existing.TotalDurationMs + metric.DurationMs,
                TotalPromptTokens = existing.TotalPromptTokens + (metric.PromptTokens ?? 0),
                TotalCompletionTokens = existing.TotalCompletionTokens + (metric.CompletionTokens ?? 0),
                FirstRequest = existing.FirstRequest,
                LastRequest = metric.Timestamp
            }
        );

        // Trim old metrics if necessary
        while (_metrics.Count > _maxStoredMetrics)
        {
            _metrics.TryDequeue(out _);
        }

        _logger.LogDebug(
            "Recorded metric: {Model} - {Duration}ms, Success: {Success}",
            metric.ModelName, metric.DurationMs, metric.Success);
    }

    /// <summary>
    /// Gets metrics for a specific model.
    /// </summary>
    public ModelMetrics? GetModelMetrics(string modelName)
    {
        return _modelMetrics.TryGetValue(modelName, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Gets aggregated metrics across all models.
    /// </summary>
    public AggregatedMetrics GetAggregatedMetrics()
    {
        var allMetrics = _metrics.ToArray();

        if (!allMetrics.Any())
        {
            return new AggregatedMetrics
            {
                TotalRequests = 0,
                SuccessfulRequests = 0,
                FailedRequests = 0,
                AverageDurationMs = 0,
                MedianDurationMs = 0,
                P95DurationMs = 0,
                P99DurationMs = 0,
                ModelMetrics = new Dictionary<string, ModelMetrics>()
            };
        }

        var successful = allMetrics.Where(m => m.Success).ToArray();
        var failed = allMetrics.Where(m => !m.Success).ToArray();
        var durations = allMetrics.Select(m => m.DurationMs).OrderBy(d => d).ToArray();

        return new AggregatedMetrics
        {
            TotalRequests = allMetrics.Length,
            SuccessfulRequests = successful.Length,
            FailedRequests = failed.Length,
            SuccessRate = allMetrics.Length > 0 
                ? (double)successful.Length / allMetrics.Length * 100 
                : 0,
            AverageDurationMs = durations.Average(),
            MedianDurationMs = GetPercentile(durations, 0.5),
            P95DurationMs = GetPercentile(durations, 0.95),
            P99DurationMs = GetPercentile(durations, 0.99),
            MinDurationMs = durations.Min(),
            MaxDurationMs = durations.Max(),
            TotalPromptTokens = allMetrics.Sum(m => m.PromptTokens ?? 0),
            TotalCompletionTokens = allMetrics.Sum(m => m.CompletionTokens ?? 0),
            ModelMetrics = new Dictionary<string, ModelMetrics>(_modelMetrics),
            FirstRequest = allMetrics.Min(m => m.Timestamp),
            LastRequest = allMetrics.Max(m => m.Timestamp)
        };
    }

    /// <summary>
    /// Gets recent metrics (last N requests).
    /// </summary>
    public IEnumerable<RequestMetric> GetRecentMetrics(int count = 10)
    {
        return _metrics.TakeLast(count);
    }

    /// <summary>
    /// Gets metrics for a specific time period.
    /// </summary>
    public IEnumerable<RequestMetric> GetMetricsForPeriod(DateTime start, DateTime end)
    {
        return _metrics.Where(m => m.Timestamp >= start && m.Timestamp <= end);
    }

    /// <summary>
    /// Gets error rate for a model.
    /// </summary>
    public double GetErrorRate(string modelName)
    {
        if (!_modelMetrics.TryGetValue(modelName, out var metrics))
            return 0.0;

        return metrics.TotalRequests > 0
            ? (double)metrics.FailedRequests / metrics.TotalRequests * 100
            : 0.0;
    }

    /// <summary>
    /// Gets average latency for a model.
    /// </summary>
    public double GetAverageLatency(string modelName)
    {
        if (!_modelMetrics.TryGetValue(modelName, out var metrics))
            return 0.0;

        return metrics.TotalRequests > 0
            ? (double)metrics.TotalDurationMs / metrics.TotalRequests
            : 0.0;
    }

    /// <summary>
    /// Gets throughput (requests per minute).
    /// </summary>
    public double GetThroughput()
    {
        var allMetrics = _metrics.ToArray();
        if (!allMetrics.Any())
            return 0.0;

        var duration = (allMetrics.Max(m => m.Timestamp) - allMetrics.Min(m => m.Timestamp)).TotalMinutes;

        return duration > 0 ? allMetrics.Length / duration : 0.0;
    }

    /// <summary>
    /// Exports metrics to JSON.
    /// </summary>
    public string ExportToJson()
    {
        var data = new
        {
            Aggregated = GetAggregatedMetrics(),
            RecentRequests = GetRecentMetrics(50)
        };

        return System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Clears all metrics.
    /// </summary>
    public void Clear()
    {
        _metrics.Clear();
        _modelMetrics.Clear();
        _logger.LogInformation("Cleared all request metrics");
    }

    // Helper method to calculate percentile
    private static long GetPercentile(long[] sortedValues, double percentile)
    {
        if (!sortedValues.Any())
            return 0;

        var index = (int)Math.Ceiling(sortedValues.Length * percentile) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Length - 1));

        return sortedValues[index];
    }
}

/// <summary>
/// Tracks a single request for metrics collection.
/// </summary>
public class RequestTracker : IDisposable
{
    private readonly RequestMetricsCollector _collector;
    private readonly string _modelName;
    private readonly string _sessionId;
    private readonly Stopwatch _stopwatch;
    private readonly DateTime _startTime;
    private bool _completed;

    internal RequestTracker(RequestMetricsCollector collector, string modelName, string sessionId)
    {
        _collector = collector;
        _modelName = modelName;
        _sessionId = sessionId;
        _stopwatch = Stopwatch.StartNew();
        _startTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Completes the request successfully.
    /// </summary>
    public void Complete(int? promptTokens = null, int? completionTokens = null)
    {
        if (_completed)
            return;

        _stopwatch.Stop();
        _completed = true;

        var metric = new RequestMetric
        {
            ModelName = _modelName,
            SessionId = _sessionId,
            Timestamp = _startTime,
            DurationMs = _stopwatch.ElapsedMilliseconds,
            Success = true,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens
        };

        _collector.RecordRequest(metric);
    }

    /// <summary>
    /// Completes the request with an error.
    /// </summary>
    public void Fail(string? errorMessage = null)
    {
        if (_completed)
            return;

        _stopwatch.Stop();
        _completed = true;

        var metric = new RequestMetric
        {
            ModelName = _modelName,
            SessionId = _sessionId,
            Timestamp = _startTime,
            DurationMs = _stopwatch.ElapsedMilliseconds,
            Success = false,
            ErrorMessage = errorMessage
        };

        _collector.RecordRequest(metric);
    }

    public void Dispose()
    {
        if (!_completed)
        {
            // If not explicitly completed, mark as failed
            Fail("Request not completed");
        }
    }
}

/// <summary>
/// Single request metric.
/// </summary>
public record RequestMetric
{
    public required string ModelName { get; init; }
    public required string SessionId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required long DurationMs { get; init; }
    public required bool Success { get; init; }
    public int? PromptTokens { get; init; }
    public int? CompletionTokens { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Metrics for a specific model.
/// </summary>
public record ModelMetrics
{
    public required string ModelName { get; init; }
    public required int TotalRequests { get; init; }
    public required int SuccessfulRequests { get; init; }
    public required int FailedRequests { get; init; }
    public required long TotalDurationMs { get; init; }
    public required int TotalPromptTokens { get; init; }
    public required int TotalCompletionTokens { get; init; }
    public required DateTime FirstRequest { get; init; }
    public required DateTime LastRequest { get; init; }

    public double AverageDurationMs => TotalRequests > 0 ? (double)TotalDurationMs / TotalRequests : 0;
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;
}

/// <summary>
/// Aggregated metrics across all models.
/// </summary>
public record AggregatedMetrics
{
    public required int TotalRequests { get; init; }
    public required int SuccessfulRequests { get; init; }
    public required int FailedRequests { get; init; }
    public double SuccessRate { get; init; }
    public required double AverageDurationMs { get; init; }
    public required long MedianDurationMs { get; init; }
    public required long P95DurationMs { get; init; }
    public required long P99DurationMs { get; init; }
    public long MinDurationMs { get; init; }
    public long MaxDurationMs { get; init; }
    public int TotalPromptTokens { get; init; }
    public int TotalCompletionTokens { get; init; }
    public required Dictionary<string, ModelMetrics> ModelMetrics { get; init; }
    public DateTime? FirstRequest { get; init; }
    public DateTime? LastRequest { get; init; }
}
