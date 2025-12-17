using Foundz.Net.Core.Planning;
using Foundz.Net.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Foundz.Net.Tests.Unit.Planning;

public class TaskPlannerTests
{
    private readonly Mock<ILogger<TaskPlanner>> _loggerMock;
    private readonly Mock<IToolRegistry> _toolRegistryMock;
    private readonly TaskPlanner _planner;

    public TaskPlannerTests()
    {
        _loggerMock = new Mock<ILogger<TaskPlanner>>();
        _toolRegistryMock = new Mock<IToolRegistry>();
        _planner = new TaskPlanner(_loggerMock.Object, _toolRegistryMock.Object);
    }

    [Fact]
    public async Task CreatePlanAsync_RefactorTask_CreatesMultiStepPlan()
    {
        // Arrange
        var taskDescription = "Refactor the user service class";
        var context = new PlanningContext
        {
            ProjectPath = "/project",
            AvailableTools = new List<string> { "analyze", "refactor", "test" }
        };

        // Act
        var plan = await _planner.CreatePlanAsync(taskDescription, context);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal(taskDescription, plan.Description);
        Assert.True(plan.Steps.Count >= 4); // Analyze, detect, apply, test
        Assert.True(plan.EstimatedTokens > 0);
        Assert.NotEqual(TaskComplexity.Simple, plan.EstimatedComplexity);
    }

    [Fact]
    public async Task CreatePlanAsync_GenerateTestTask_CreatesTestGenerationPlan()
    {
        // Arrange
        var taskDescription = "Generate tests for the Calculator class";
        var context = new PlanningContext();

        // Act
        var plan = await _planner.CreatePlanAsync(taskDescription, context);

        // Assert
        Assert.Equal(3, plan.Steps.Count);
        Assert.Contains(plan.Steps, s => s.Description.Contains("Analyze"));
        Assert.Contains(plan.Steps, s => s.Description.Contains("Generate"));
        Assert.Contains(plan.Steps, s => s.Description.Contains("Run"));
    }

    [Fact]
    public async Task CreatePlanAsync_DocumentTask_CreatesDocumentationPlan()
    {
        // Arrange
        var taskDescription = "Document the API endpoints";
        var context = new PlanningContext();

        // Act
        var plan = await _planner.CreatePlanAsync(taskDescription, context);

        // Assert
        Assert.Equal(2, plan.Steps.Count);
        Assert.Contains(plan.Steps, s => s.ToolName == "parse_code");
        Assert.Contains(plan.Steps, s => s.ToolName == "generate_docs");
    }

    [Fact]
    public async Task CreatePlanAsync_SimpleTask_CreatesSingleStepPlan()
    {
        // Arrange
        var taskDescription = "Read the configuration file";
        var context = new PlanningContext();

        // Act
        var plan = await _planner.CreatePlanAsync(taskDescription, context);

        // Assert
        Assert.Single(plan.Steps);
        Assert.Equal(taskDescription, plan.Steps[0].Description);
        Assert.Equal(TaskComplexity.Simple, plan.EstimatedComplexity);
    }

    [Fact]
    public async Task CreatePlanAsync_SetsDependencies()
    {
        // Arrange
        var taskDescription = "Refactor the code";
        var context = new PlanningContext();

        // Act
        var plan = await _planner.CreatePlanAsync(taskDescription, context);

        // Assert
        Assert.NotEmpty(plan.Dependencies);
        // Each step (except first) should depend on previous
        for (int i = 1; i < plan.Steps.Count; i++)
        {
            Assert.True(plan.Dependencies.ContainsKey(plan.Steps[i].Id));
        }
    }

    [Fact]
    public async Task CreatePlanAsync_FirstStepIsReady()
    {
        // Arrange
        var taskDescription = "Refactor the code";
        var context = new PlanningContext();

        // Act
        var plan = await _planner.CreatePlanAsync(taskDescription, context);

        // Assert
        Assert.Equal(StepStatus.Ready, plan.Steps[0].Status);
    }

    [Fact]
    public async Task CreatePlanAsync_EstimatesTokenUsage()
    {
        // Arrange
        var taskDescription = "Refactor all services";
        var context = new PlanningContext();

        // Act
        var plan = await _planner.CreatePlanAsync(taskDescription, context);

        // Assert
        Assert.True(plan.EstimatedTokens >= 1000);
        Assert.True(plan.EstimatedTokens == 1000 + (plan.Steps.Count * 500));
    }

    [Theory]
    [InlineData("simple task", TaskComplexity.Simple)]
    [InlineData("analyze and test this", TaskComplexity.Simple)]
    [InlineData("refactor all classes", TaskComplexity.Medium)]
    [InlineData("analyze all files and refactor multiple modules", TaskComplexity.Complex)]
    public async Task CreatePlanAsync_AnalyzesComplexityCorrectly(string task, TaskComplexity expected)
    {
        // Arrange
        var context = new PlanningContext();

        // Act
        var plan = await _planner.CreatePlanAsync(task, context);

        // Assert
        Assert.Equal(expected, plan.EstimatedComplexity);
    }

    [Fact]
    public void UpdatePlan_CompletedStepSuccess_MarksAsCompleted()
    {
        // Arrange
        var plan = new TaskPlan
        {
            Steps = new List<TaskStep>
            {
                new() { Id = "step1", Status = StepStatus.InProgress },
                new() { Id = "step2", Status = StepStatus.Pending }
            },
            Dependencies = new Dictionary<string, List<string>>
            {
                ["step2"] = new List<string> { "step1" }
            }
        };

        // Act
        var updated = _planner.UpdatePlan(plan, plan.Steps[0], success: true);

        // Assert
        Assert.Equal(StepStatus.Completed, updated.Steps[0].Status);
        Assert.NotNull(updated.Steps[0].CompletedAt);
    }

    [Fact]
    public void UpdatePlan_CompletedStepSuccess_MarksDependentStepsReady()
    {
        // Arrange
        var plan = new TaskPlan
        {
            Steps = new List<TaskStep>
            {
                new() { Id = "step1", Status = StepStatus.InProgress },
                new() { Id = "step2", Status = StepStatus.Pending }
            },
            Dependencies = new Dictionary<string, List<string>>
            {
                ["step2"] = new List<string> { "step1" }
            }
        };

        // Act
        var updated = _planner.UpdatePlan(plan, plan.Steps[0], success: true);

        // Assert
        Assert.Equal(StepStatus.Ready, updated.Steps[1].Status);
    }

    [Fact]
    public void UpdatePlan_CompletedStepFailure_MarksAsFailed()
    {
        // Arrange
        var plan = new TaskPlan
        {
            Steps = new List<TaskStep>
            {
                new() { Id = "step1", Status = StepStatus.InProgress }
            }
        };

        // Act
        var updated = _planner.UpdatePlan(plan, plan.Steps[0], success: false);

        // Assert
        Assert.Equal(StepStatus.Failed, updated.Steps[0].Status);
    }

    [Fact]
    public void UpdatePlan_CompletedStepFailure_BlocksDependentSteps()
    {
        // Arrange
        var plan = new TaskPlan
        {
            Steps = new List<TaskStep>
            {
                new() { Id = "step1", Status = StepStatus.InProgress },
                new() { Id = "step2", Status = StepStatus.Pending }
            },
            Dependencies = new Dictionary<string, List<string>>
            {
                ["step2"] = new List<string> { "step1" }
            }
        };

        // Act
        var updated = _planner.UpdatePlan(plan, plan.Steps[0], success: false);

        // Assert
        Assert.Equal(StepStatus.Blocked, updated.Steps[1].Status);
    }

    [Fact]
    public void GetNextSteps_ReturnsReadySteps()
    {
        // Arrange
        var plan = new TaskPlan
        {
            Steps = new List<TaskStep>
            {
                new() { Id = "1", Status = StepStatus.Ready, Priority = 1 },
                new() { Id = "2", Status = StepStatus.Ready, Priority = 2 },
                new() { Id = "3", Status = StepStatus.Pending, Priority = 3 }
            }
        };

        // Act
        var nextSteps = _planner.GetNextSteps(plan);

        // Assert
        Assert.Equal(2, nextSteps.Count);
        Assert.All(nextSteps, s => Assert.Equal(StepStatus.Ready, s.Status));
    }

    [Fact]
    public void GetNextSteps_RespectsMaxParallel()
    {
        // Arrange
        var plan = new TaskPlan
        {
            Steps = new List<TaskStep>
            {
                new() { Status = StepStatus.Ready, Priority = 1 },
                new() { Status = StepStatus.Ready, Priority = 2 },
                new() { Status = StepStatus.Ready, Priority = 3 },
                new() { Status = StepStatus.Ready, Priority = 4 }
            }
        };

        // Act
        var nextSteps = _planner.GetNextSteps(plan, maxParallel: 2);

        // Assert
        Assert.Equal(2, nextSteps.Count);
    }

    [Fact]
    public void GetNextSteps_OrdersByPriority()
    {
        // Arrange
        var plan = new TaskPlan
        {
            Steps = new List<TaskStep>
            {
                new() { Id = "low", Status = StepStatus.Ready, Priority = 3 },
                new() { Id = "high", Status = StepStatus.Ready, Priority = 1 },
                new() { Id = "mid", Status = StepStatus.Ready, Priority = 2 }
            }
        };

        // Act
        var nextSteps = _planner.GetNextSteps(plan);

        // Assert
        Assert.Equal("high", nextSteps[0].Id);
        Assert.Equal("mid", nextSteps[1].Id);
        Assert.Equal("low", nextSteps[2].Id);
    }

    [Fact]
    public void IsPlanComplete_AllCompleted_ReturnsTrue()
    {
        // Arrange
        var plan = new TaskPlan
        {
            Steps = new List<TaskStep>
            {
                new() { Status = StepStatus.Completed },
                new() { Status = StepStatus.Completed },
                new() { Status = StepStatus.Skipped }
            }
        };

        // Act
        var isComplete = _planner.IsPlanComplete(plan);

        // Assert
        Assert.True(isComplete);
    }

    [Fact]
    public void IsPlanComplete_SomeNotCompleted_ReturnsFalse()
    {
        // Arrange
        var plan = new TaskPlan
        {
            Steps = new List<TaskStep>
            {
                new() { Status = StepStatus.Completed },
                new() { Status = StepStatus.Pending }
            }
        };

        // Act
        var isComplete = _planner.IsPlanComplete(plan);

        // Assert
        Assert.False(isComplete);
    }

    [Fact]
    public void GetProgress_CalculatesCorrectly()
    {
        // Arrange
        var plan = new TaskPlan
        {
            Steps = new List<TaskStep>
            {
                new() { Status = StepStatus.Completed },
                new() { Status = StepStatus.Completed },
                new() { Status = StepStatus.Failed },
                new() { Status = StepStatus.InProgress },
                new() { Status = StepStatus.Pending }
            }
        };

        // Act
        var progress = _planner.GetProgress(plan);

        // Assert
        Assert.Equal(5, progress.Total);
        Assert.Equal(2, progress.Completed);
        Assert.Equal(1, progress.Failed);
        Assert.Equal(1, progress.InProgress);
        Assert.Equal(40.0, progress.Percentage);
    }

    [Fact]
    public void GetProgress_EmptyPlan_ReturnsZeroPercentage()
    {
        // Arrange
        var plan = new TaskPlan { Steps = new List<TaskStep>() };

        // Act
        var progress = _planner.GetProgress(plan);

        // Assert
        Assert.Equal(0, progress.Total);
        Assert.Equal(0.0, progress.Percentage);
    }

    [Fact]
    public void PlanProgress_ToString_FormatsCorrectly()
    {
        // Arrange
        var progress = new PlanProgress
        {
            Total = 10,
            Completed = 5,
            Failed = 1,
            Blocked = 2,
            Percentage = 50.0
        };

        // Act
        var formatted = progress.ToString();

        // Assert
        Assert.Contains("5/10", formatted);
        Assert.Contains("50.0%", formatted);
        Assert.Contains("Failed: 1", formatted);
        Assert.Contains("Blocked: 2", formatted);
    }
}
