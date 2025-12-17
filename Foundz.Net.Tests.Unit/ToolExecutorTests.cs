using Foundz.Net.Core.ToolRegistry;
using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Foundz.Net.Tests.Unit;

public class ToolExecutorTests
{
    private readonly Mock<IToolRegistry> _mockRegistry;
    private readonly Mock<ILogger<ToolExecutor>> _mockLogger;
    private readonly ToolExecutor _executor;

    public ToolExecutorTests()
    {
        _mockRegistry = new Mock<IToolRegistry>();
        _mockLogger = new Mock<ILogger<ToolExecutor>>();
        _executor = new ToolExecutor(_mockRegistry.Object, _mockLogger.Object, defaultTimeoutSeconds: 5);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidTool_ShouldReturnSuccessResult()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            Id = "test_call_1",
            Name = "test_tool",
            Arguments = new Dictionary<string, object>()
        };

        var mockTool = CreateMockTool("test_tool", success: true);
        _mockRegistry.Setup(r => r.GetTool("test_tool")).Returns(mockTool);

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ToolCallId.Should().Be("test_call_1");
        result.ToolName.Should().Be("test_tool");
        result.ExecutionTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownTool_ShouldReturnErrorResult()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            Id = "test_call_1",
            Name = "unknown_tool",
            Arguments = new Dictionary<string, object>()
        };

        _mockRegistry.Setup(r => r.GetTool("unknown_tool")).Returns((ITool?)null);

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidArguments_ShouldReturnErrorResult()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            Id = "test_call_1",
            Name = "test_tool",
            Arguments = new Dictionary<string, object> { ["invalid"] = "value" }
        };

        var mockTool = CreateMockTool("test_tool", success: true, validateArgs: false);
        _mockRegistry.Setup(r => r.GetTool("test_tool")).Returns(mockTool);

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Invalid arguments");
    }

    [Fact]
    public async Task ExecuteParallelAsync_ShouldExecuteAllTools()
    {
        // Arrange
        var toolCalls = new List<ToolCall>
        {
            new() { Id = "call1", Name = "tool1", Arguments = new() },
            new() { Id = "call2", Name = "tool2", Arguments = new() },
            new() { Id = "call3", Name = "tool3", Arguments = new() }
        };

        _mockRegistry.Setup(r => r.GetTool(It.IsAny<string>()))
            .Returns((string name) => CreateMockTool(name, success: true));

        // Act
        var results = await _executor.ExecuteParallelAsync(toolCalls);

        // Assert
        results.Should().HaveCount(3);
        results.All(r => r.Success).Should().BeTrue();
    }

    [Fact]
    public void CanExecuteInParallel_WithSafeTools_ShouldReturnTrue()
    {
        // Arrange
        var toolCalls = new List<ToolCall>
        {
            new() { Id = "call1", Name = "read_tool", Arguments = new() },
            new() { Id = "call2", Name = "list_tool", Arguments = new() }
        };

        var safeTool = CreateMockTool("read_tool", dangerLevel: DangerLevel.Safe);
        _mockRegistry.Setup(r => r.GetTool(It.IsAny<string>())).Returns(safeTool);

        // Act
        var result = _executor.CanExecuteInParallel(toolCalls);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanExecuteInParallel_WithDangerousTools_ShouldReturnFalse()
    {
        // Arrange
        var toolCalls = new List<ToolCall>
        {
            new() { Id = "call1", Name = "delete_tool", Arguments = new() }
        };

        var dangerousTool = CreateMockTool("delete_tool", dangerLevel: DangerLevel.Danger);
        _mockRegistry.Setup(r => r.GetTool("delete_tool")).Returns(dangerousTool);

        // Act
        var result = _executor.CanExecuteInParallel(toolCalls);

        // Assert
        result.Should().BeFalse();
    }

    private ITool CreateMockTool(
        string name,
        bool success = true,
        bool validateArgs = true,
        DangerLevel dangerLevel = DangerLevel.Safe)
    {
        var mockTool = new Mock<ITool>();
        mockTool.Setup(t => t.Name).Returns(name);
        mockTool.Setup(t => t.Description).Returns($"Test tool {name}");
        mockTool.Setup(t => t.Category).Returns(ToolCategory.FileOperations);
        mockTool.Setup(t => t.DangerLevel).Returns(dangerLevel);
        mockTool.Setup(t => t.RequiresConfirmation).Returns(false);
        mockTool.Setup(t => t.ValidateArgsAsync(It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validateArgs);
        mockTool.Setup(t => t.ExecuteAsync(It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolResult
            {
                ToolCallId = Guid.NewGuid().ToString(),
                ToolName = name,
                Success = success,
                Output = success ? "Success" : null,
                Error = success ? null : "Error occurred",
                ExecutionTimeMs = 10
            });

        return mockTool.Object;
    }
}
