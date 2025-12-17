using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.Planning;

/// <summary>
/// Tracks execution progress and provides real-time updates.
/// Estimates remaining time and reports completion status.
/// </summary>
public class ProgressTracker
{
    private readonly ILogger<ProgressTracker> _logger;
    private readonly Dictionary<string, ProgressState> _states;

    public event EventHandler<ProgressEventArgs>? ProgressChanged;

    public ProgressTracker(ILogger<ProgressTracker> logger)
    {
        _logger = logger;
        _states = new Dictionary<string, ProgressState>();
    }

    /// <summary>
    /// Start tracking progress for a task
    /// </summary>
    public void StartTracking(string taskId, string description, int totalSteps)
    {
        var state = new ProgressState
        {
            TaskId = taskId,
            Description = description,
            TotalSteps = totalSteps,
            CompletedSteps = 0,
            StartTime = DateTime.UtcNow,
            CurrentPhase = "Starting"
        };

        _states[taskId] = state;

        _logger.LogInformation("Started tracking task: {TaskId} - {Description} ({Steps} steps)",
            taskId, description, totalSteps);

        RaiseProgressChanged(state);
    }

    /// <summary>
    /// Update progress for a step completion
    /// </summary>
    public void UpdateProgress(
        string taskId,
        string stepDescription,
        int stepNumber,
        string? message = null)
    {
        if (!_states.TryGetValue(taskId, out var state))
        {
            _logger.LogWarning("Task not found: {TaskId}", taskId);
            return;
        }

        state.CompletedSteps = stepNumber;
        state.CurrentPhase = stepDescription;
        state.LastUpdate = DateTime.UtcNow;
        state.Message = message;

        // Update estimated time remaining
        var elapsed = state.LastUpdate - state.StartTime;
        if (state.CompletedSteps > 0)
        {
            var avgTimePerStep = elapsed / state.CompletedSteps;
            var remainingSteps = state.TotalSteps - state.CompletedSteps;
            state.EstimatedTimeRemaining = avgTimePerStep * remainingSteps;
        }

        _logger.LogDebug(
            "Progress: {TaskId} - Step {Current}/{Total}: {Phase}",
            taskId, state.CompletedSteps, state.TotalSteps, stepDescription);

        RaiseProgressChanged(state);
    }

    /// <summary>
    /// Mark task as complete
    /// </summary>
    public void Complete(string taskId, string? message = null)
    {
        if (!_states.TryGetValue(taskId, out var state))
        {
            _logger.LogWarning("Task not found: {TaskId}", taskId);
            return;
        }

        state.CompletedSteps = state.TotalSteps;
        state.CurrentPhase = "Completed";
        state.EndTime = DateTime.UtcNow;
        state.Message = message;
        state.IsComplete = true;

        var duration = state.EndTime.Value - state.StartTime;

        _logger.LogInformation(
            "Task completed: {TaskId} - Duration: {Duration}ms",
            taskId, duration.TotalMilliseconds);

        RaiseProgressChanged(state);
    }

    /// <summary>
    /// Mark task as failed
    /// </summary>
    public void Fail(string taskId, string error)
    {
        if (!_states.TryGetValue(taskId, out var state))
        {
            _logger.LogWarning("Task not found: {TaskId}", taskId);
            return;
        }

        state.CurrentPhase = "Failed";
        state.EndTime = DateTime.UtcNow;
        state.Message = error;
        state.IsFailed = true;

        _logger.LogError("Task failed: {TaskId} - {Error}", taskId, error);

        RaiseProgressChanged(state);
    }

    /// <summary>
    /// Get current progress state
    /// </summary>
    public ProgressState? GetProgress(string taskId)
    {
        return _states.TryGetValue(taskId, out var state) ? state : null;
    }

    /// <summary>
    /// Get all active tasks
    /// </summary>
    public List<ProgressState> GetActiveTasks()
    {
        return _states.Values
            .Where(s => !s.IsComplete && !s.IsFailed)
            .OrderByDescending(s => s.StartTime)
            .ToList();
    }

    /// <summary>
    /// Clear completed/failed tasks
    /// </summary>
    public void Cleanup(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var toRemove = _states
            .Where(kvp => 
                (kvp.Value.IsComplete || kvp.Value.IsFailed) &&
                kvp.Value.EndTime < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var taskId in toRemove)
        {
            _states.Remove(taskId);
        }

        if (toRemove.Any())
        {
            _logger.LogDebug("Cleaned up {Count} old task states", toRemove.Count);
        }
    }

    /// <summary>
    /// Format progress as text
    /// </summary>
    public string FormatProgress(string taskId)
    {
        var state = GetProgress(taskId);
        if (state == null)
            return "Task not found";

        var percentage = state.TotalSteps > 0 
            ? (state.CompletedSteps * 100.0 / state.TotalSteps) 
            : 0;

        var parts = new List<string>
        {
            $"[{percentage:F1}%] {state.Description}",
            $"Step {state.CompletedSteps}/{state.TotalSteps}: {state.CurrentPhase}"
        };

        if (state.EstimatedTimeRemaining != null && !state.IsComplete)
        {
            parts.Add($"ETA: {FormatDuration(state.EstimatedTimeRemaining.Value)}");
        }

        if (state.IsComplete)
        {
            var duration = (state.EndTime!.Value - state.StartTime).TotalSeconds;
            parts.Add($"Completed in {duration:F1}s");
        }

        if (state.IsFailed)
        {
            parts.Add($"Failed: {state.Message}");
        }

        return string.Join(" | ", parts);
    }

    private void RaiseProgressChanged(ProgressState state)
    {
        ProgressChanged?.Invoke(this, new ProgressEventArgs
        {
            TaskId = state.TaskId,
            Description = state.Description,
            CurrentStep = state.CompletedSteps,
            TotalSteps = state.TotalSteps,
            Phase = state.CurrentPhase,
            Message = state.Message,
            Percentage = state.TotalSteps > 0 
                ? (state.CompletedSteps * 100.0 / state.TotalSteps) 
                : 0,
            IsComplete = state.IsComplete,
            IsFailed = state.IsFailed
        });
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 60)
            return $"{duration.TotalSeconds:F0}s";
        if (duration.TotalMinutes < 60)
            return $"{duration.TotalMinutes:F1}m";
        return $"{duration.TotalHours:F1}h";
    }
}

/// <summary>
/// Progress state for a task
/// </summary>
public class ProgressState
{
    public string TaskId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime LastUpdate { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public bool IsComplete { get; set; }
    public bool IsFailed { get; set; }

    public double Percentage => TotalSteps > 0 
        ? (CompletedSteps * 100.0 / TotalSteps) 
        : 0;

    public TimeSpan Elapsed => (EndTime ?? DateTime.UtcNow) - StartTime;
}

/// <summary>
/// Progress change event arguments
/// </summary>
public class ProgressEventArgs : EventArgs
{
    public string TaskId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string Phase { get; set; } = string.Empty;
    public string? Message { get; set; }
    public double Percentage { get; set; }
    public bool IsComplete { get; set; }
    public bool IsFailed { get; set; }

    public override string ToString()
    {
        return $"[{Percentage:F1}%] {Description} - {Phase}";
    }
}
