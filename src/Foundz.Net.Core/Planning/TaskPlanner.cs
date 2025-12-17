using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.Planning;

/// <summary>
/// Plans complex tasks by breaking them into steps with dependencies and estimates.
/// Implements intelligent task decomposition and execution planning.
/// </summary>
public class TaskPlanner
{
    private readonly ILogger<TaskPlanner> _logger;
    private readonly IToolRegistry _toolRegistry;

    public TaskPlanner(ILogger<TaskPlanner> logger, IToolRegistry toolRegistry)
    {
        _logger = logger;
        _toolRegistry = toolRegistry;
    }

    /// <summary>
    /// Create a plan for a complex task
    /// </summary>
    public async Task<TaskPlan> CreatePlanAsync(
        string taskDescription,
        PlanningContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating plan for task: {Task}", taskDescription);

        var plan = new TaskPlan
        {
            Description = taskDescription,
            CreatedAt = DateTime.UtcNow
        };

        // Analyze task complexity
        var complexity = AnalyzeComplexity(taskDescription);
        plan.EstimatedComplexity = complexity;

        // Break down into steps
        var steps = await DecomposeTaskAsync(taskDescription, context, cancellationToken);
        plan.Steps = steps;

        // Detect dependencies between steps
        plan.Dependencies = DetectDependencies(steps);

        // Estimate token usage
        plan.EstimatedTokens = EstimateTokenUsage(steps);

        // Prioritize steps
        PrioritizeSteps(plan);

        _logger.LogInformation(
            "Plan created: {StepCount} steps, complexity: {Complexity}, estimated tokens: {Tokens}",
            plan.Steps.Count,
            plan.EstimatedComplexity,
            plan.EstimatedTokens);

        return plan;
    }

    /// <summary>
    /// Update plan based on execution results
    /// </summary>
    public TaskPlan UpdatePlan(TaskPlan plan, TaskStep completedStep, bool success)
    {
        var step = plan.Steps.FirstOrDefault(s => s.Id == completedStep.Id);
        if (step != null)
        {
            step.Status = success ? StepStatus.Completed : StepStatus.Failed;
            step.CompletedAt = DateTime.UtcNow;

            _logger.LogDebug("Step {Id} marked as {Status}", step.Id, step.Status);
        }

        // Update dependent steps
        if (success)
        {
            // Mark dependent steps as ready
            var dependentSteps = plan.Dependencies
                .Where(d => d.Value.Contains(completedStep.Id))
                .Select(d => d.Key);

            foreach (var depStepId in dependentSteps)
            {
                var depStep = plan.Steps.FirstOrDefault(s => s.Id == depStepId);
                if (depStep != null && depStep.Status == StepStatus.Pending)
                {
                    // Check if all dependencies are met
                    if (AllDependenciesMet(plan, depStepId))
                    {
                        depStep.Status = StepStatus.Ready;
                    }
                }
            }
        }
        else
        {
            // Mark dependent steps as blocked
            var dependentSteps = plan.Dependencies
                .Where(d => d.Value.Contains(completedStep.Id))
                .Select(d => d.Key);

            foreach (var depStepId in dependentSteps)
            {
                var depStep = plan.Steps.FirstOrDefault(s => s.Id == depStepId);
                if (depStep != null)
                {
                    depStep.Status = StepStatus.Blocked;
                }
            }
        }

        return plan;
    }

    /// <summary>
    /// Get next step(s) ready for execution
    /// </summary>
    public List<TaskStep> GetNextSteps(TaskPlan plan, int maxParallel = 3)
    {
        return plan.Steps
            .Where(s => s.Status == StepStatus.Ready)
            .OrderBy(s => s.Priority)
            .Take(maxParallel)
            .ToList();
    }

    /// <summary>
    /// Check if plan is complete
    /// </summary>
    public bool IsPlanComplete(TaskPlan plan)
    {
        return plan.Steps.All(s => 
            s.Status == StepStatus.Completed || 
            s.Status == StepStatus.Skipped);
    }

    /// <summary>
    /// Get plan progress
    /// </summary>
    public PlanProgress GetProgress(TaskPlan plan)
    {
        var total = plan.Steps.Count;
        var completed = plan.Steps.Count(s => s.Status == StepStatus.Completed);
        var failed = plan.Steps.Count(s => s.Status == StepStatus.Failed);
        var blocked = plan.Steps.Count(s => s.Status == StepStatus.Blocked);

        return new PlanProgress
        {
            Total = total,
            Completed = completed,
            Failed = failed,
            Blocked = blocked,
            InProgress = plan.Steps.Count(s => s.Status == StepStatus.InProgress),
            Percentage = total > 0 ? (completed * 100.0 / total) : 0
        };
    }

    private async Task<List<TaskStep>> DecomposeTaskAsync(
        string taskDescription,
        PlanningContext context,
        CancellationToken cancellationToken)
    {
        var steps = new List<TaskStep>();

        // Simple rule-based decomposition
        // In a real implementation, this would use AI to intelligently break down tasks

        if (taskDescription.Contains("refactor", StringComparison.OrdinalIgnoreCase))
        {
            steps.Add(new TaskStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Analyze code structure",
                ToolName = "analyze_complexity",
                Status = StepStatus.Ready,
                Priority = 1
            });

            steps.Add(new TaskStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Find code duplications",
                ToolName = "detect_duplication",
                Status = StepStatus.Pending,
                Priority = 2
            });

            steps.Add(new TaskStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Apply refactoring",
                ToolName = "extract_method",
                Status = StepStatus.Pending,
                Priority = 3
            });

            steps.Add(new TaskStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Run tests",
                ToolName = "run_tests",
                Status = StepStatus.Pending,
                Priority = 4
            });
        }
        else if (taskDescription.Contains("test", StringComparison.OrdinalIgnoreCase) &&
                 taskDescription.Contains("generate", StringComparison.OrdinalIgnoreCase))
        {
            steps.Add(new TaskStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Analyze code to test",
                ToolName = "parse_code",
                Status = StepStatus.Ready,
                Priority = 1
            });

            steps.Add(new TaskStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Generate test cases",
                ToolName = "generate_test",
                Status = StepStatus.Pending,
                Priority = 2
            });

            steps.Add(new TaskStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Run generated tests",
                ToolName = "run_tests",
                Status = StepStatus.Pending,
                Priority = 3
            });
        }
        else if (taskDescription.Contains("document", StringComparison.OrdinalIgnoreCase))
        {
            steps.Add(new TaskStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Analyze code structure",
                ToolName = "parse_code",
                Status = StepStatus.Ready,
                Priority = 1
            });

            steps.Add(new TaskStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Generate documentation",
                ToolName = "generate_docs",
                Status = StepStatus.Pending,
                Priority = 2
            });
        }
        else
        {
            // Default: single-step plan
            steps.Add(new TaskStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = taskDescription,
                ToolName = null, // AI will determine
                Status = StepStatus.Ready,
                Priority = 1
            });
        }

        await Task.CompletedTask;
        return steps;
    }

    private TaskComplexity AnalyzeComplexity(string taskDescription)
    {
        // Simple heuristic-based complexity analysis
        var keywords = taskDescription.ToLower();
        var complexityScore = 0;

        // Indicators of complexity
        if (keywords.Contains("refactor")) complexityScore += 3;
        if (keywords.Contains("all") || keywords.Contains("entire")) complexityScore += 2;
        if (keywords.Contains("multiple")) complexityScore += 2;
        if (keywords.Contains("complex")) complexityScore += 2;
        if (keywords.Contains("analyze")) complexityScore += 1;
        if (keywords.Contains("test")) complexityScore += 1;
        if (keywords.Contains("generate")) complexityScore += 1;

        return complexityScore switch
        {
            <= 2 => TaskComplexity.Simple,
            <= 5 => TaskComplexity.Medium,
            _ => TaskComplexity.Complex
        };
    }

    private Dictionary<string, List<string>> DetectDependencies(List<TaskStep> steps)
    {
        var dependencies = new Dictionary<string, List<string>>();

        // Simple sequential dependencies
        // In a real implementation, this would analyze tool inputs/outputs
        for (int i = 1; i < steps.Count; i++)
        {
            dependencies[steps[i].Id] = new List<string> { steps[i - 1].Id };
        }

        return dependencies;
    }

    private int EstimateTokenUsage(List<TaskStep> steps)
    {
        // Rough estimation based on step count and complexity
        var baseTokens = 1000; // Base conversation tokens
        var perStepTokens = 500; // Tokens per step (tool call + response)
        
        return baseTokens + (steps.Count * perStepTokens);
    }

    private void PrioritizeSteps(TaskPlan plan)
    {
        // Priority already set during decomposition
        // Could be enhanced with more sophisticated logic
    }

    private bool AllDependenciesMet(TaskPlan plan, string stepId)
    {
        if (!plan.Dependencies.TryGetValue(stepId, out var deps))
            return true;

        return deps.All(depId =>
        {
            var depStep = plan.Steps.FirstOrDefault(s => s.Id == depId);
            return depStep?.Status == StepStatus.Completed;
        });
    }
}

/// <summary>
/// Planning context with available information
/// </summary>
public class PlanningContext
{
    public string ProjectPath { get; set; } = string.Empty;
    public List<string> AvailableFiles { get; set; } = new();
    public List<string> AvailableTools { get; set; } = new();
    public int MaxTokens { get; set; } = 150000;
}

/// <summary>
/// Complete task plan
/// </summary>
public class TaskPlan
{
    public string Description { get; set; } = string.Empty;
    public List<TaskStep> Steps { get; set; } = new();
    public Dictionary<string, List<string>> Dependencies { get; set; } = new();
    public TaskComplexity EstimatedComplexity { get; set; }
    public int EstimatedTokens { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Individual task step
/// </summary>
public class TaskStep
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ToolName { get; set; }
    public Dictionary<string, object>? Arguments { get; set; }
    public StepStatus Status { get; set; }
    public int Priority { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Step execution status
/// </summary>
public enum StepStatus
{
    Pending,    // Not ready yet
    Ready,      // Ready to execute
    InProgress, // Currently executing
    Completed,  // Successfully completed
    Failed,     // Execution failed
    Blocked,    // Blocked by failed dependency
    Skipped     // Intentionally skipped
}

/// <summary>
/// Task complexity classification
/// </summary>
public enum TaskComplexity
{
    Simple,  // 1-2 steps
    Medium,  // 3-5 steps
    Complex  // 6+ steps
}

/// <summary>
/// Plan execution progress
/// </summary>
public class PlanProgress
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
    public int Blocked { get; set; }
    public int InProgress { get; set; }
    public double Percentage { get; set; }

    public override string ToString()
    {
        return $"{Completed}/{Total} ({Percentage:F1}%) - Failed: {Failed}, Blocked: {Blocked}";
    }
}
