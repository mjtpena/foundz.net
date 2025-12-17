using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;

namespace Foundz.Net.Tests.Unit.Shared.Interfaces;

public class AgentResponseTests
{
    [Fact]
    public void AgentResponse_Properties_CanBeSet()
    {
        // Arrange & Act
        var response = new AgentResponse
        {
            Content = "Task completed successfully",
            ToolResults = new List<ToolResult>
            {
                new ToolResult
                {
                    ToolCallId = "call_1",
                    ToolName = "file_read",
                    Success = true,
                    Output = "File content",
                    ExecutionTimeMs = 150
                }
            },
            TokenUsage = new TokenUsage { PromptTokens = 500, CompletionTokens = 200 },
            IterationCount = 3,
            HitMaxIterations = false
        };

        // Assert
        Assert.Equal("Task completed successfully", response.Content);
        Assert.NotNull(response.ToolResults);
        Assert.Single(response.ToolResults);
        Assert.NotNull(response.TokenUsage);
        Assert.Equal(3, response.IterationCount);
        Assert.False(response.HitMaxIterations);
    }

    [Fact]
    public void AgentResponse_EmptyToolResults_WorksCorrectly()
    {
        // Arrange & Act
        var response = new AgentResponse
        {
            Content = "Simple response without tools",
            ToolResults = new List<ToolResult>(),
            TokenUsage = new TokenUsage { PromptTokens = 100, CompletionTokens = 50 },
            IterationCount = 1,
            HitMaxIterations = false
        };

        // Assert
        Assert.Empty(response.ToolResults);
        Assert.Equal(1, response.IterationCount);
    }

    [Fact]
    public void AgentResponse_MaxIterationsHit_WorksCorrectly()
    {
        // Arrange & Act
        var response = new AgentResponse
        {
            Content = "Max iterations reached",
            ToolResults = new List<ToolResult>(),
            TokenUsage = new TokenUsage { PromptTokens = 1000, CompletionTokens = 500 },
            IterationCount = 15,
            HitMaxIterations = true
        };

        // Assert
        Assert.True(response.HitMaxIterations);
        Assert.Equal(15, response.IterationCount);
    }
}

public class AgentEventTests
{
    [Fact]
    public void AgentEvent_StateChanged_WorksCorrectly()
    {
        // Arrange & Act
        var event1 = new AgentEvent
        {
            Type = AgentEventType.StateChanged,
            State = AgentState.Thinking
        };

        // Assert
        Assert.Equal(AgentEventType.StateChanged, event1.Type);
        Assert.Equal(AgentState.Thinking, event1.State);
        Assert.Null(event1.Content);
        Assert.Null(event1.ToolCall);
        Assert.Null(event1.ToolResult);
    }

    [Fact]
    public void AgentEvent_ContentDelta_WorksCorrectly()
    {
        // Arrange & Act
        var event1 = new AgentEvent
        {
            Type = AgentEventType.ContentDelta,
            Content = "Partial text"
        };

        // Assert
        Assert.Equal(AgentEventType.ContentDelta, event1.Type);
        Assert.Equal("Partial text", event1.Content);
    }

    [Fact]
    public void AgentEvent_ToolCallStarted_WorksCorrectly()
    {
        // Arrange & Act
        var toolCall = new ToolCall
        {
            Id = "call_1",
            Name = "file_read",
            Arguments = new Dictionary<string, object> { { "path", "test.txt" } }
        };
        var event1 = new AgentEvent
        {
            Type = AgentEventType.ToolCallStarted,
            ToolCall = toolCall
        };

        // Assert
        Assert.Equal(AgentEventType.ToolCallStarted, event1.Type);
        Assert.NotNull(event1.ToolCall);
        Assert.Equal("file_read", event1.ToolCall.Name);
    }

    [Fact]
    public void AgentEvent_ToolCallCompleted_WorksCorrectly()
    {
        // Arrange & Act
        var toolResult = new ToolResult
        {
            ToolCallId = "call_1",
            ToolName = "file_read",
            Success = true,
            Output = "File content here",
            ExecutionTimeMs = 100
        };
        var event1 = new AgentEvent
        {
            Type = AgentEventType.ToolCallCompleted,
            ToolResult = toolResult
        };

        // Assert
        Assert.Equal(AgentEventType.ToolCallCompleted, event1.Type);
        Assert.NotNull(event1.ToolResult);
        Assert.True(event1.ToolResult.Success);
    }

    [Fact]
    public void AgentEvent_Error_WorksCorrectly()
    {
        // Arrange & Act
        var event1 = new AgentEvent
        {
            Type = AgentEventType.Error,
            Content = "An error occurred",
            State = AgentState.Error
        };

        // Assert
        Assert.Equal(AgentEventType.Error, event1.Type);
        Assert.Equal("An error occurred", event1.Content);
        Assert.Equal(AgentState.Error, event1.State);
    }
}

public class AgentEventTypeTests
{
    [Fact]
    public void AgentEventType_AllValues_AreValid()
    {
        // Assert
        Assert.Equal(0, (int)AgentEventType.StateChanged);
        Assert.Equal(1, (int)AgentEventType.ContentDelta);
        Assert.Equal(2, (int)AgentEventType.ToolCallStarted);
        Assert.Equal(3, (int)AgentEventType.ToolCallCompleted);
        Assert.Equal(4, (int)AgentEventType.IterationCompleted);
        Assert.Equal(5, (int)AgentEventType.Error);
    }

    [Fact]
    public void AgentEventType_CanBeSwitched()
    {
        // Arrange
        var types = new[]
        {
            AgentEventType.StateChanged,
            AgentEventType.ContentDelta,
            AgentEventType.ToolCallStarted,
            AgentEventType.ToolCallCompleted,
            AgentEventType.IterationCompleted,
            AgentEventType.Error
        };

        // Act & Assert
        foreach (var type in types)
        {
            var description = type switch
            {
                AgentEventType.StateChanged => "State changed",
                AgentEventType.ContentDelta => "Content delta",
                AgentEventType.ToolCallStarted => "Tool call started",
                AgentEventType.ToolCallCompleted => "Tool call completed",
                AgentEventType.IterationCompleted => "Iteration completed",
                AgentEventType.Error => "Error",
                _ => "Unknown"
            };

            Assert.NotEqual("Unknown", description);
        }
    }
}

public class AgentStateTests
{
    [Fact]
    public void AgentState_AllValues_AreValid()
    {
        // Assert
        Assert.Equal(0, (int)AgentState.Idle);
        Assert.Equal(1, (int)AgentState.Thinking);
        Assert.Equal(2, (int)AgentState.ToolExecution);
        Assert.Equal(3, (int)AgentState.WaitingConfirmation);
        Assert.Equal(4, (int)AgentState.Streaming);
        Assert.Equal(5, (int)AgentState.Error);
        Assert.Equal(6, (int)AgentState.Completed);
    }

    [Fact]
    public void AgentState_CanBeCompared()
    {
        // Arrange
        var idle = AgentState.Idle;
        var thinking = AgentState.Thinking;
        var completed = AgentState.Completed;

        // Assert
        Assert.Equal(AgentState.Idle, idle);
        Assert.NotEqual(idle, thinking);
        Assert.NotEqual(thinking, completed);
    }

    [Fact]
    public void AgentState_CanBeSwitched()
    {
        // Arrange
        var states = new[]
        {
            AgentState.Idle,
            AgentState.Thinking,
            AgentState.ToolExecution,
            AgentState.WaitingConfirmation,
            AgentState.Streaming,
            AgentState.Error,
            AgentState.Completed
        };

        // Act & Assert
        foreach (var state in states)
        {
            var description = state switch
            {
                AgentState.Idle => "Idle",
                AgentState.Thinking => "Thinking",
                AgentState.ToolExecution => "Tool execution",
                AgentState.WaitingConfirmation => "Waiting confirmation",
                AgentState.Streaming => "Streaming",
                AgentState.Error => "Error",
                AgentState.Completed => "Completed",
                _ => "Unknown"
            };

            Assert.NotEqual("Unknown", description);
        }
    }
}
