using Foundz.Net.Core.Planning;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Foundz.Net.Tests.Unit.Planning;

public class ProgressTrackerTests
{
    private readonly Mock<ILogger<ProgressTracker>> _loggerMock;
    private readonly ProgressTracker _tracker;

    public ProgressTrackerTests()
    {
        _loggerMock = new Mock<ILogger<ProgressTracker>>();
        _tracker = new ProgressTracker(_loggerMock.Object);
    }

    [Fact]
    public void StartTracking_CreatesNewProgressState()
    {
        // Arrange
        var taskId = "task-1";
        var description = "Test task";
        var totalSteps = 5;

        // Act
        _tracker.StartTracking(taskId, description, totalSteps);
        var progress = _tracker.GetProgress(taskId);

        // Assert
        Assert.NotNull(progress);
        Assert.Equal(taskId, progress.TaskId);
        Assert.Equal(description, progress.Description);
        Assert.Equal(totalSteps, progress.TotalSteps);
        Assert.Equal(0, progress.CompletedSteps);
        Assert.Equal("Starting", progress.CurrentPhase);
    }

    [Fact]
    public void StartTracking_RaisesProgressChangedEvent()
    {
        // Arrange
        var eventRaised = false;
        _tracker.ProgressChanged += (sender, args) => { eventRaised = true; };

        // Act
        _tracker.StartTracking("task-1", "Test", 5);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void UpdateProgress_UpdatesStateCorrectly()
    {
        // Arrange
        var taskId = "task-1";
        _tracker.StartTracking(taskId, "Test", 5);

        // Act
        _tracker.UpdateProgress(taskId, "Step 1", 1, "Processing...");
        var progress = _tracker.GetProgress(taskId);

        // Assert
        Assert.NotNull(progress);
        Assert.Equal(1, progress.CompletedSteps);
        Assert.Equal("Step 1", progress.CurrentPhase);
        Assert.Equal("Processing...", progress.Message);
    }

    [Fact]
    public void UpdateProgress_EstimatesTimeRemaining()
    {
        // Arrange
        var taskId = "task-1";
        _tracker.StartTracking(taskId, "Test", 10);
        Thread.Sleep(100);

        // Act
        _tracker.UpdateProgress(taskId, "Step 5", 5);
        var progress = _tracker.GetProgress(taskId);

        // Assert
        Assert.NotNull(progress);
        Assert.NotNull(progress.EstimatedTimeRemaining);
        Assert.True(progress.EstimatedTimeRemaining.Value.TotalMilliseconds > 0);
    }

    [Fact]
    public void UpdateProgress_NonExistentTask_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        _tracker.UpdateProgress("non-existent", "Step", 1);
        Assert.True(true);
    }

    [Fact]
    public void Complete_MarksTaskAsComplete()
    {
        // Arrange
        var taskId = "task-1";
        _tracker.StartTracking(taskId, "Test", 5);

        // Act
        _tracker.Complete(taskId, "All done!");
        var progress = _tracker.GetProgress(taskId);

        // Assert
        Assert.NotNull(progress);
        Assert.True(progress.IsComplete);
        Assert.Equal(5, progress.CompletedSteps);
        Assert.Equal("Completed", progress.CurrentPhase);
        Assert.Equal("All done!", progress.Message);
        Assert.NotNull(progress.EndTime);
    }

    [Fact]
    public void Complete_NonExistentTask_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        _tracker.Complete("non-existent");
        Assert.True(true);
    }

    [Fact]
    public void Fail_MarksTaskAsFailed()
    {
        // Arrange
        var taskId = "task-1";
        _tracker.StartTracking(taskId, "Test", 5);

        // Act
        _tracker.Fail(taskId, "Something went wrong");
        var progress = _tracker.GetProgress(taskId);

        // Assert
        Assert.NotNull(progress);
        Assert.True(progress.IsFailed);
        Assert.Equal("Failed", progress.CurrentPhase);
        Assert.Equal("Something went wrong", progress.Message);
        Assert.NotNull(progress.EndTime);
    }

    [Fact]
    public void Fail_NonExistentTask_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        _tracker.Fail("non-existent", "Error");
        Assert.True(true);
    }

    [Fact]
    public void GetProgress_NonExistentTask_ReturnsNull()
    {
        // Act
        var progress = _tracker.GetProgress("non-existent");

        // Assert
        Assert.Null(progress);
    }

    [Fact]
    public void GetActiveTasks_ReturnsOnlyActiveTasks()
    {
        // Arrange
        _tracker.StartTracking("task-1", "Active 1", 5);
        _tracker.StartTracking("task-2", "Active 2", 5);
        _tracker.StartTracking("task-3", "Completed", 5);
        _tracker.Complete("task-3");
        _tracker.StartTracking("task-4", "Failed", 5);
        _tracker.Fail("task-4", "Error");

        // Act
        var activeTasks = _tracker.GetActiveTasks();

        // Assert
        Assert.Equal(2, activeTasks.Count);
        Assert.All(activeTasks, t => Assert.False(t.IsComplete || t.IsFailed));
    }

    [Fact]
    public void GetActiveTasks_OrdersByStartTime()
    {
        // Arrange
        _tracker.StartTracking("task-1", "First", 5);
        Thread.Sleep(10);
        _tracker.StartTracking("task-2", "Second", 5);
        Thread.Sleep(10);
        _tracker.StartTracking("task-3", "Third", 5);

        // Act
        var activeTasks = _tracker.GetActiveTasks();

        // Assert
        Assert.Equal("task-3", activeTasks[0].TaskId);
        Assert.Equal("task-2", activeTasks[1].TaskId);
        Assert.Equal("task-1", activeTasks[2].TaskId);
    }

    [Fact]
    public void Cleanup_RemovesOldCompletedTasks()
    {
        // Arrange
        _tracker.StartTracking("old-task", "Old", 5);
        _tracker.StartTracking("recent-task", "Recent", 5);
        
        // Complete old task and manually set end time (using reflection)
        _tracker.Complete("old-task");
        var states = typeof(ProgressTracker)
            .GetField("_states", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(_tracker) as Dictionary<string, ProgressState>;
        
        states!["old-task"].EndTime = DateTime.UtcNow.AddHours(-2);
        _tracker.Complete("recent-task");

        // Act
        _tracker.Cleanup(TimeSpan.FromHours(1));

        // Assert
        Assert.Null(_tracker.GetProgress("old-task"));
        Assert.NotNull(_tracker.GetProgress("recent-task"));
    }

    [Fact]
    public void Cleanup_DoesNotRemoveActiveTasks()
    {
        // Arrange
        _tracker.StartTracking("active-task", "Active", 5);

        // Act
        _tracker.Cleanup(TimeSpan.FromSeconds(1));

        // Assert
        Assert.NotNull(_tracker.GetProgress("active-task"));
    }

    [Fact]
    public void FormatProgress_FormatsCorrectly()
    {
        // Arrange
        var taskId = "task-1";
        _tracker.StartTracking(taskId, "Test Task", 10);
        _tracker.UpdateProgress(taskId, "Processing", 5);

        // Act
        var formatted = _tracker.FormatProgress(taskId);

        // Assert
        Assert.Contains("50.0%", formatted);
        Assert.Contains("Test Task", formatted);
        Assert.Contains("Step 5/10", formatted);
        Assert.Contains("Processing", formatted);
    }

    [Fact]
    public void FormatProgress_NonExistentTask_ReturnsNotFound()
    {
        // Act
        var formatted = _tracker.FormatProgress("non-existent");

        // Assert
        Assert.Equal("Task not found", formatted);
    }

    [Fact]
    public void FormatProgress_CompletedTask_ShowsDuration()
    {
        // Arrange
        var taskId = "task-1";
        _tracker.StartTracking(taskId, "Test", 5);
        Thread.Sleep(100);
        _tracker.Complete(taskId);

        // Act
        var formatted = _tracker.FormatProgress(taskId);

        // Assert
        Assert.Contains("Completed in", formatted);
    }

    [Fact]
    public void FormatProgress_FailedTask_ShowsError()
    {
        // Arrange
        var taskId = "task-1";
        _tracker.StartTracking(taskId, "Test", 5);
        _tracker.Fail(taskId, "Test error");

        // Act
        var formatted = _tracker.FormatProgress(taskId);

        // Assert
        Assert.Contains("Failed: Test error", formatted);
    }

    [Fact]
    public void ProgressState_CalculatesPercentageCorrectly()
    {
        // Arrange
        var state = new ProgressState
        {
            TotalSteps = 10,
            CompletedSteps = 7
        };

        // Act
        var percentage = state.Percentage;

        // Assert
        Assert.Equal(70.0, percentage);
    }

    [Fact]
    public void ProgressState_ElapsedTime_CalculatesCorrectly()
    {
        // Arrange
        var state = new ProgressState
        {
            StartTime = DateTime.UtcNow.AddSeconds(-5)
        };

        // Act
        var elapsed = state.Elapsed;

        // Assert
        Assert.True(elapsed.TotalSeconds >= 5);
        Assert.True(elapsed.TotalSeconds < 6);
    }

    [Fact]
    public void ProgressEventArgs_ToStringFormatsCorrectly()
    {
        // Arrange
        var args = new ProgressEventArgs
        {
            Percentage = 75.5,
            Description = "Test Task",
            Phase = "Processing"
        };

        // Act
        var formatted = args.ToString();

        // Assert
        Assert.Contains("75.5%", formatted);
        Assert.Contains("Test Task", formatted);
        Assert.Contains("Processing", formatted);
    }
}
