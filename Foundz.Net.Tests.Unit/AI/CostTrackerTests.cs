using Foundz.Net.Core.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Foundz.Net.Tests.Unit.AI;

public class CostTrackerTests
{
    private readonly Mock<ILogger<CostTracker>> _mockLogger;
    private readonly CostTracker _tracker;

    public CostTrackerTests()
    {
        _mockLogger = new Mock<ILogger<CostTracker>>();
        _tracker = new CostTracker(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CostTracker(null!));
    }

    [Fact]
    public void RecordRequest_CalculatesCostCorrectly()
    {
        // Arrange
        var sessionId = "session-1";
        var modelName = "claude-3-5-sonnet-20241022";
        var timestamp = DateTime.UtcNow;

        // Act
        _tracker.RecordRequest(sessionId, modelName, 1000, 500, timestamp);

        // Assert
        var sessionCost = _tracker.GetSessionCost(sessionId);
        Assert.True(sessionCost > 0);
        
        // Expected: (1000/1000 * $3.00) + (500/1000 * $15.00) = $3.00 + $7.50 = $10.50
        Assert.Equal(10.50m, sessionCost, 2);
    }

    [Fact]
    public void RecordRequest_WithMultipleRequests_AggregatesCosts()
    {
        // Arrange
        var sessionId = "session-1";
        var timestamp = DateTime.UtcNow;

        // Act
        _tracker.RecordRequest(sessionId, "claude-3-5-sonnet-20241022", 1000, 500, timestamp);
        _tracker.RecordRequest(sessionId, "claude-3-5-sonnet-20241022", 2000, 1000, timestamp);

        // Assert
        var sessionCost = _tracker.GetSessionCost(sessionId);
        // First: $10.50, Second: $21.00, Total: $31.50
        Assert.Equal(31.50m, sessionCost, 2);
    }

    [Fact]
    public void RecordRequest_WithUnknownModel_UsesDefaultPricing()
    {
        // Arrange
        var sessionId = "session-1";
        var modelName = "unknown-model";
        var timestamp = DateTime.UtcNow;

        // Act
        _tracker.RecordRequest(sessionId, modelName, 1000, 500, timestamp);

        // Assert
        var sessionCost = _tracker.GetSessionCost(sessionId);
        // Default: (1000/1000 * $1.00) + (500/1000 * $3.00) = $1.00 + $1.50 = $2.50
        Assert.Equal(2.50m, sessionCost, 2);
    }

    [Fact]
    public void GetSessionCost_ForNonExistentSession_ReturnsZero()
    {
        // Act
        var cost = _tracker.GetSessionCost("non-existent");

        // Assert
        Assert.Equal(0m, cost);
    }

    [Fact]
    public void GetSessionSummary_ReturnsCorrectBreakdown()
    {
        // Arrange
        var sessionId = "session-1";
        var timestamp = DateTime.UtcNow;
        _tracker.RecordRequest(sessionId, "claude-3-5-sonnet-20241022", 1000, 500, timestamp);
        _tracker.RecordRequest(sessionId, "gpt-4o", 2000, 1000, timestamp);

        // Act
        var summary = _tracker.GetSessionSummary(sessionId);

        // Assert
        Assert.Equal(sessionId, summary.SessionId);
        Assert.Equal(2, summary.RequestCount);
        Assert.Equal(3000, summary.TotalPromptTokens);
        Assert.Equal(1500, summary.TotalCompletionTokens);
        Assert.Equal(2, summary.ModelBreakdown.Count);
        Assert.True(summary.TotalCost > 0);
    }

    [Fact]
    public void GetSessionSummary_ForNonExistentSession_ReturnsEmptySummary()
    {
        // Act
        var summary = _tracker.GetSessionSummary("non-existent");

        // Assert
        Assert.Equal("non-existent", summary.SessionId);
        Assert.Equal(0m, summary.TotalCost);
        Assert.Equal(0, summary.RequestCount);
        Assert.Empty(summary.ModelBreakdown);
    }

    [Fact]
    public void GetTotalCost_AcrossMultipleSessions_CalculatesCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        _tracker.RecordRequest("session-1", "claude-3-5-sonnet-20241022", 1000, 500, timestamp);
        _tracker.RecordRequest("session-2", "gpt-4o", 1000, 500, timestamp);

        // Act
        var totalCost = _tracker.GetTotalCost();

        // Assert
        // Session1: $10.50, Session2: (1000/1000 * $5.00) + (500/1000 * $15.00) = $12.50
        Assert.Equal(23.00m, totalCost, 2);
    }

    [Fact]
    public void GetCostForPeriod_FiltersCorrectly()
    {
        // Arrange
        var start = DateTime.UtcNow;
        _tracker.RecordRequest("session-1", "claude-3-5-sonnet-20241022", 1000, 500, start);
        _tracker.RecordRequest("session-1", "claude-3-5-sonnet-20241022", 1000, 500, start.AddMinutes(10));
        _tracker.RecordRequest("session-1", "claude-3-5-sonnet-20241022", 1000, 500, start.AddMinutes(20));

        // Act
        var cost = _tracker.GetCostForPeriod(start, start.AddMinutes(11));

        // Assert
        // Should include first two requests only: $10.50 * 2 = $21.00
        Assert.Equal(21.00m, cost, 2);
    }

    [Fact]
    public void GetUsageStatistics_ReturnsCompleteStats()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        _tracker.RecordRequest("session-1", "claude-3-5-sonnet-20241022", 1000, 500, timestamp);
        _tracker.RecordRequest("session-2", "gpt-4o", 2000, 1000, timestamp);

        // Act
        var stats = _tracker.GetUsageStatistics();

        // Assert
        Assert.Equal(2, stats.TotalRequests);
        Assert.Equal(3000, stats.TotalPromptTokens);
        Assert.Equal(1500, stats.TotalCompletionTokens);
        Assert.Equal(2, stats.SessionCount);
        Assert.Equal(2, stats.ModelBreakdown.Count);
        Assert.True(stats.TotalCost > 0);
    }

    [Fact]
    public void GetUsageStatistics_WithNoData_ReturnsEmpty()
    {
        // Act
        var stats = _tracker.GetUsageStatistics();

        // Assert
        Assert.Equal(0, stats.TotalRequests);
        Assert.Equal(0m, stats.TotalCost);
        Assert.Empty(stats.ModelBreakdown);
    }

    [Fact]
    public void RegisterModelPricing_UpdatesPricing()
    {
        // Arrange
        var sessionId = "session-1";
        var modelName = "custom-model";
        var timestamp = DateTime.UtcNow;

        // Act
        _tracker.RegisterModelPricing(modelName, 5.0m, 10.0m);
        _tracker.RecordRequest(sessionId, modelName, 1000, 1000, timestamp);

        // Assert
        var cost = _tracker.GetSessionCost(sessionId);
        // (1000/1000 * $5.00) + (1000/1000 * $10.00) = $15.00
        Assert.Equal(15.00m, cost, 2);
    }

    [Fact]
    public void ExportToJson_ReturnsValidJson()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        _tracker.RecordRequest("session-1", "claude-3-5-sonnet-20241022", 1000, 500, timestamp);

        // Act
        var json = _tracker.ExportToJson();

        // Assert
        Assert.NotEmpty(json);
        Assert.Contains("TotalCost", json);
        Assert.Contains("Sessions", json);
    }

    [Fact]
    public void ClearSession_RemovesSessionData()
    {
        // Arrange
        var sessionId = "session-1";
        var timestamp = DateTime.UtcNow;
        _tracker.RecordRequest(sessionId, "claude-3-5-sonnet-20241022", 1000, 500, timestamp);
        var initialTotal = _tracker.GetTotalCost();

        // Act
        _tracker.ClearSession(sessionId);

        // Assert
        var sessionCost = _tracker.GetSessionCost(sessionId);
        Assert.Equal(0m, sessionCost);
        Assert.Equal(0m, _tracker.GetTotalCost());
    }

    [Fact]
    public void ClearSession_ForNonExistentSession_DoesNotThrow()
    {
        // Act & Assert - should not throw
        _tracker.ClearSession("non-existent");
    }

    [Fact]
    public void Reset_ClearsAllData()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        _tracker.RecordRequest("session-1", "claude-3-5-sonnet-20241022", 1000, 500, timestamp);
        _tracker.RecordRequest("session-2", "gpt-4o", 1000, 500, timestamp);

        // Act
        _tracker.Reset();

        // Assert
        Assert.Equal(0m, _tracker.GetTotalCost());
        Assert.Equal(0m, _tracker.GetSessionCost("session-1"));
        Assert.Equal(0m, _tracker.GetSessionCost("session-2"));
        var stats = _tracker.GetUsageStatistics();
        Assert.Equal(0, stats.TotalRequests);
    }

    [Theory]
    [InlineData("gpt-4", 30.0, 60.0)]
    [InlineData("gpt-4-turbo", 10.0, 30.0)]
    [InlineData("gpt-4o", 5.0, 15.0)]
    [InlineData("gpt-4o-mini", 0.15, 0.60)]
    public void DefaultPricing_ForGPTModels_IsCorrect(string modelName, decimal expectedInput, decimal expectedOutput)
    {
        // Arrange
        var sessionId = "session-1";
        var timestamp = DateTime.UtcNow;

        // Act
        _tracker.RecordRequest(sessionId, modelName, 1000, 1000, timestamp);

        // Assert
        var cost = _tracker.GetSessionCost(sessionId);
        var expectedCost = (1000 / 1000m) * expectedInput + (1000 / 1000m) * expectedOutput;
        Assert.Equal(expectedCost, cost, 2);
    }

    [Theory]
    [InlineData("claude-3-5-sonnet-20241022", 3.0, 15.0)]
    [InlineData("claude-3-opus-20240229", 15.0, 75.0)]
    [InlineData("claude-3-haiku-20240307", 0.25, 1.25)]
    public void DefaultPricing_ForClaudeModels_IsCorrect(string modelName, decimal expectedInput, decimal expectedOutput)
    {
        // Arrange
        var sessionId = "session-1";
        var timestamp = DateTime.UtcNow;

        // Act
        _tracker.RecordRequest(sessionId, modelName, 1000, 1000, timestamp);

        // Assert
        var cost = _tracker.GetSessionCost(sessionId);
        var expectedCost = (1000 / 1000m) * expectedInput + (1000 / 1000m) * expectedOutput;
        Assert.Equal(expectedCost, cost, 2);
    }
}
