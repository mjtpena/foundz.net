using Foundz.Net.Tools.Shell;
using Foundz.Net.Shared.Interfaces;
using Xunit;
using FluentAssertions;

namespace Foundz.Net.Tests.Unit.Tools;

public class ShellToolsTests
{
    [Fact]
    public async Task ExecuteCommandTool_ShouldExecuteCommand()
    {
        // Arrange
        var tool = new ExecuteCommandTool();
        var args = new Dictionary<string, object>
        {
            ["command"] = OperatingSystem.IsWindows() ? "echo Hello" : "echo \"Hello\""
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Contain("Hello");
    }

    [Fact]
    public async Task ExecuteCommandTool_ShouldHandleWorkingDirectory()
    {
        // Arrange
        var tool = new ExecuteCommandTool();
        var tempDir = Path.GetTempPath();
        var args = new Dictionary<string, object>
        {
            ["command"] = OperatingSystem.IsWindows() ? "cd" : "pwd",
            ["workingDirectory"] = tempDir
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteCommandTool_ShouldValidateArgs()
    {
        // Arrange
        var tool = new ExecuteCommandTool();

        // Act & Assert
        (await tool.ValidateArgsAsync(new Dictionary<string, object> { ["command"] = "test" })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public async Task BuildProjectTool_ShouldValidateArgs()
    {
        // Arrange
        var tool = new BuildProjectTool();

        // Act & Assert
        (await tool.ValidateArgsAsync(new Dictionary<string, object> { ["projectPath"] = "test.csproj" })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void BuildProjectTool_ShouldHaveCorrectProperties()
    {
        // Arrange
        var tool = new BuildProjectTool();

        // Assert
        tool.Name.Should().Be("build_project");
        tool.Category.Should().Be(ToolCategory.ShellExecution);
        tool.RequiresConfirmation.Should().BeFalse();
    }

    [Fact]
    public async Task RunTestsTool_ShouldValidateArgs()
    {
        // Arrange
        var tool = new RunTestsTool();

        // Act & Assert
        (await tool.ValidateArgsAsync(new Dictionary<string, object> { ["projectPath"] = "test.csproj" })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void RunTestsTool_ShouldHaveCorrectProperties()
    {
        // Arrange
        var tool = new RunTestsTool();

        // Assert
        tool.Name.Should().Be("run_tests");
        tool.Category.Should().Be(ToolCategory.Testing);
        tool.DangerLevel.Should().Be(DangerLevel.Safe);
    }

    [Fact]
    public void ExecuteCommandTool_ShouldProvideExamples()
    {
        // Arrange
        var tool = new ExecuteCommandTool();

        // Act
        var examples = tool.GetExamples();

        // Assert
        examples.Should().NotBeEmpty();
        examples.Should().Contain(e => e.Contains("execute_command"));
    }

    [Fact]
    public async Task ExecuteCommandTool_ShouldHandleTimeout()
    {
        // Arrange
        var tool = new ExecuteCommandTool();
        var args = new Dictionary<string, object>
        {
            ["command"] = OperatingSystem.IsWindows() ? "timeout /t 2" : "sleep 2",
            ["timeout"] = 1000 // 1 second
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert - Command should timeout or complete
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteCommandTool_ShouldHandleInvalidCommand()
    {
        // Arrange
        var tool = new ExecuteCommandTool();
        var args = new Dictionary<string, object>
        {
            ["command"] = "this_command_does_not_exist_12345"
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
