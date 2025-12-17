using Foundz.Net.Core.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Foundz.Net.Tests.Unit.AI;

public class RequestMetricsCollectorTests
{
    private readonly Mock<ILogger<RequestMetricsCollector>> _mockLogger;
    private readonly RequestMetricsCollector _collector;

    public RequestMetricsCollectorTests()
    {
        _mockLogger = new Mock<ILogger<RequestMetricsCollector>>();
        _collector = new RequestMetricsCollector(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RequestMetricsCollector(null!)
        );
    }

    [Fact]
    public void StartRequest_ReturnsTracker()
    {
        // Act
        var tracker = _collector.StartRequest("test-model", "session-1");

        // Assert
        Assert.NotNull(tracker);
    }

    [Fact]
    public void RequestTracker_Complete_RecordsMetric()
    {
        // Arrange
        var tracker = _collector.StartRequest("test-model", "session-1");

        // Act
        tracker.Complete(promptTokens: 100, completionTokens: 50);

        // Assert
        var metrics = _collector.GetModelMetrics("test-model");
        Assert.NotNull(metrics);
        Assert.Equal(1, metrics.TotalRequests);
        Assert.Equal(1, metrics.SuccessfulRequests);
        Assert.Equal(0, metrics.FailedRequests);
        Assert.Equal(100, metrics.TotalPromptTokens);
        Assert.Equal(50, metrics.TotalCompletionTokens);
    }

    [Fact]
    public void RequestTracker_Fail_RecordsFailure()
    {
        // Arrange
        var tracker = _collector.StartRequest("test-model", "session-1");

        // Act
        tracker.Fail("Test error");

        // Assert
        var metrics = _collector.GetModelMetrics("test-model");
        Assert.NotNull(metrics);
        Assert.Equal(1, metrics.TotalRequests);
        Assert.Equal(0, metrics.SuccessfulRequests);
        Assert.Equal(1, metrics.FailedRequests);
    }

    [Fact]
    public void RequestTracker_Dispose_WithoutComplete_RecordsFailure()
    {
        // Arrange & Act
        using (var tracker = _collector.StartRequest("test-model", "session-1"))
        {
            // Dispose without calling Complete or Fail
        }

        // Assert
        var metrics = _collector.GetModelMetrics("test-model");
        Assert.NotNull(metrics);
        Assert.Equal(1, metrics.FailedRequests);
    }

    [Fact]
    public void RequestTracker_MultipleComplete_OnlyRecordsFirst()
    {
        // Arrange
        var tracker = _collector.StartRequest("test-model", "session-1");

        // Act
        tracker.Complete(100, 50);
        tracker.Complete(200, 100); // Should be ignored

        // Assert
        var metrics = _collector.GetModelMetrics("test-model");
        Assert.NotNull(metrics);
        Assert.Equal(1, metrics.TotalRequests);
    }

    [Fact]
    public void GetModelMetrics_ForNonExistentModel_ReturnsNull()
    {
        // Act
        var metrics = _collector.GetModelMetrics("non-existent");

        // Assert
        Assert.Null(metrics);
    }

    [Fact]
    public void GetModelMetrics_AggregatesCorrectly()
    {
        // Arrange
        var tracker1 = _collector.StartRequest("test-model", "session-1");
        var tracker2 = _collector.StartRequest("test-model", "session-2");

        // Act
        tracker1.Complete(100, 50);
        tracker2.Complete(200, 100);

        // Assert
        var metrics = _collector.GetModelMetrics("test-model");
        Assert.NotNull(metrics);
        Assert.Equal(2, metrics.TotalRequests);
        Assert.Equal(300, metrics.TotalPromptTokens);
        Assert.Equal(150, metrics.TotalCompletionTokens);
        Assert.Equal(100.0, metrics.SuccessRate);
    }

    [Fact]
    public void GetAggregatedMetrics_WithNoData_ReturnsEmpty()
    {
        // Act
        var metrics = _collector.GetAggregatedMetrics();

        // Assert
        Assert.Equal(0, metrics.TotalRequests);
        Assert.Equal(0.0, metrics.AverageDurationMs);
        Assert.Empty(metrics.ModelMetrics);
    }

    [Fact]
    public void GetAggregatedMetrics_CalculatesCorrectly()
    {
        // Arrange
        var tracker1 = _collector.StartRequest("model-1", "session-1");
        var tracker2 = _collector.StartRequest("model-2", "session-1");
        var tracker3 = _collector.StartRequest("model-1", "session-2");

        // Act
        Thread.Sleep(10); // Ensure some duration
        tracker1.Complete(100, 50);
        tracker2.Complete(200, 100);
        tracker3.Fail("Error");

        var metrics = _collector.GetAggregatedMetrics();

        // Assert
        Assert.Equal(3, metrics.TotalRequests);
        Assert.Equal(2, metrics.SuccessfulRequests);
        Assert.Equal(1, metrics.FailedRequests);
        Assert.True(metrics.SuccessRate > 0 && metrics.SuccessRate < 100);
        Assert.True(metrics.AverageDurationMs > 0);
        Assert.Equal(2, metrics.ModelMetrics.Count);
    }

    [Fact]
    public void GetAggregatedMetrics_CalculatesPercentiles()
    {
        // Arrange
        for (int i = 0; i < 100; i++)
        {
            var tracker = _collector.StartRequest("test-model", "session-1");
            Thread.Sleep(i % 10); // Variable duration
            tracker.Complete(100, 50);
        }

        // Act
        var metrics = _collector.GetAggregatedMetrics();

        // Assert
        Assert.True(metrics.MedianDurationMs >= 0);
        Assert.True(metrics.P95DurationMs >= metrics.MedianDurationMs);
        Assert.True(metrics.P99DurationMs >= metrics.P95DurationMs);
        Assert.True(metrics.MinDurationMs <= metrics.MedianDurationMs);
        Assert.True(metrics.MaxDurationMs >= metrics.P99DurationMs);
    }

    [Fact]
    public void GetRecentMetrics_ReturnsLastN()
    {
        // Arrange
        for (int i = 0; i < 20; i++)
        {
            var tracker = _collector.StartRequest("test-model", $"session-{i}");
            tracker.Complete(100, 50);
        }

        // Act
        var recent = _collector.GetRecentMetrics(10).ToList();

        // Assert
        Assert.Equal(10, recent.Count);
    }

    [Fact]
    public void GetMetricsForPeriod_FiltersCorrectly()
    {
        // Arrange
        var start = DateTime.UtcNow;
        
        var tracker1 = _collector.StartRequest("test-model", "session-1");
        tracker1.Complete(100, 50);
        
        Thread.Sleep(100);
        var midpoint = DateTime.UtcNow;
        
        var tracker2 = _collector.StartRequest("test-model", "session-2");
        tracker2.Complete(200, 100);
        
        var end = DateTime.UtcNow;

        // Act
        var metricsAll = _collector.GetMetricsForPeriod(start, end).ToList();
        var metricsFirst = _collector.GetMetricsForPeriod(start, midpoint).ToList();

        // Assert
        Assert.Equal(2, metricsAll.Count);
        Assert.Single(metricsFirst);
    }

    [Fact]
    public void GetErrorRate_CalculatesCorrectly()
    {
        // Arrange
        var model = "test-model";
        var tracker1 = _collector.StartRequest(model, "session-1");
        var tracker2 = _collector.StartRequest(model, "session-2");
        var tracker3 = _collector.StartRequest(model, "session-3");
        var tracker4 = _collector.StartRequest(model, "session-4");

        tracker1.Complete(100, 50);
        tracker2.Fail("Error");
        tracker3.Complete(100, 50);
        tracker4.Fail("Error");

        // Act
        var errorRate = _collector.GetErrorRate(model);

        // Assert
        Assert.Equal(50.0, errorRate, 1);
    }

    [Fact]
    public void GetErrorRate_ForNonExistentModel_ReturnsZero()
    {
        // Act
        var errorRate = _collector.GetErrorRate("non-existent");

        // Assert
        Assert.Equal(0.0, errorRate);
    }

    [Fact]
    public void GetAverageLatency_CalculatesCorrectly()
    {
        // Arrange
        var model = "test-model";
        
        for (int i = 0; i < 10; i++)
        {
            var tracker = _collector.StartRequest(model, $"session-{i}");
            Thread.Sleep(10);
            tracker.Complete(100, 50);
        }

        // Act
        var avgLatency = _collector.GetAverageLatency(model);

        // Assert
        Assert.True(avgLatency > 0);
    }

    [Fact]
    public void GetAverageLatency_ForNonExistentModel_ReturnsZero()
    {
        // Act
        var avgLatency = _collector.GetAverageLatency("non-existent");

        // Assert
        Assert.Equal(0.0, avgLatency);
    }

    [Fact]
    public void GetThroughput_CalculatesRequestsPerMinute()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var tracker = _collector.StartRequest("test-model", $"session-{i}");
            tracker.Complete(100, 50);
            Thread.Sleep(10);
        }

        // Act
        var throughput = _collector.GetThroughput();

        // Assert
        Assert.True(throughput > 0);
    }

    [Fact]
    public void GetThroughput_WithNoData_ReturnsZero()
    {
        // Act
        var throughput = _collector.GetThroughput();

        // Assert
        Assert.Equal(0.0, throughput);
    }

    [Fact]
    public void ExportToJson_ReturnsValidJson()
    {
        // Arrange
        var tracker = _collector.StartRequest("test-model", "session-1");
        tracker.Complete(100, 50);

        // Act
        var json = _collector.ExportToJson();

        // Assert
        Assert.NotEmpty(json);
        Assert.Contains("Aggregated", json);
        Assert.Contains("RecentRequests", json);
    }

    [Fact]
    public void Clear_RemovesAllMetrics()
    {
        // Arrange
        var tracker1 = _collector.StartRequest("model-1", "session-1");
        var tracker2 = _collector.StartRequest("model-2", "session-2");
        tracker1.Complete(100, 50);
        tracker2.Complete(200, 100);

        // Act
        _collector.Clear();

        // Assert
        var metrics = _collector.GetAggregatedMetrics();
        Assert.Equal(0, metrics.TotalRequests);
        Assert.Null(_collector.GetModelMetrics("model-1"));
        Assert.Null(_collector.GetModelMetrics("model-2"));
    }

    [Fact]
    public void Constructor_WithMaxStoredMetrics_LimitsStorage()
    {
        // Arrange
        var collector = new RequestMetricsCollector(_mockLogger.Object, maxStoredMetrics: 10);

        // Act - Add 20 metrics
        for (int i = 0; i < 20; i++)
        {
            var tracker = collector.StartRequest("test-model", $"session-{i}");
            tracker.Complete(100, 50);
        }

        // Assert - Should only have last 10
        var recent = collector.GetRecentMetrics(20).ToList();
        Assert.True(recent.Count <= 10);
    }

    [Fact]
    public void ModelMetrics_CalculatesAverageDurationCorrectly()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var tracker = _collector.StartRequest("test-model", $"session-{i}");
            Thread.Sleep(10);
            tracker.Complete(100, 50);
        }

        // Act
        var metrics = _collector.GetModelMetrics("test-model");

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.AverageDurationMs > 0);
        Assert.Equal(metrics.TotalDurationMs / (double)metrics.TotalRequests, metrics.AverageDurationMs, 2);
    }

    [Fact]
    public void ModelMetrics_TracksFirstAndLastRequest()
    {
        // Arrange
        var tracker1 = _collector.StartRequest("test-model", "session-1");
        tracker1.Complete(100, 50);
        
        Thread.Sleep(100);
        
        var tracker2 = _collector.StartRequest("test-model", "session-2");
        tracker2.Complete(200, 100);

        // Act
        var metrics = _collector.GetModelMetrics("test-model");

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.LastRequest > metrics.FirstRequest);
    }
}
