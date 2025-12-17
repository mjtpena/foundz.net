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
            ["command"] = "dotnet",
            ["arguments"] = "--version"
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteCommandTool_ShouldHandleWorkingDirectory()
    {
        // Arrange
        var tool = new ExecuteCommandTool();
        var tempDir = Path.GetTempPath();
        var args = new Dictionary<string, object>
        {
            ["command"] = "dotnet",
            ["arguments"] = "--version",
            ["working_directory"] = tempDir
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

        // Act & Assert - BuildProjectTool has no required args, always valid
        (await tool.ValidateArgsAsync(new Dictionary<string, object> { ["path"] = "test.csproj" })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeTrue();
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

        // Act & Assert - RunTestsTool has no required args, always valid
        (await tool.ValidateArgsAsync(new Dictionary<string, object> { ["path"] = "test.csproj" })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeTrue();
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
            ["command"] = "dotnet",
            ["arguments"] = "--help",
            ["timeout_seconds"] = 5
        };

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert - Command should complete successfully
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
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
