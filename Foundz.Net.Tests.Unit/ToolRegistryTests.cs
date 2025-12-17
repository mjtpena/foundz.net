using Foundz.Net.Core.ToolRegistry;
using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Foundz.Net.Tests.Unit;

public class ToolRegistryTests
{
    private readonly Mock<ILogger<ToolRegistry>> _mockLogger;
    private readonly ToolRegistry _registry;

    public ToolRegistryTests()
    {
        _mockLogger = new Mock<ILogger<ToolRegistry>>();
        _registry = new ToolRegistry(_mockLogger.Object);
    }

    [Fact]
    public void RegisterTool_ShouldAddToolToRegistry()
    {
        // Arrange
        var mockTool = CreateMockTool("test_tool");

        // Act
        _registry.RegisterTool(mockTool);

        // Assert
        _registry.Count.Should().Be(1);
        var retrievedTool = _registry.GetTool("test_tool");
        retrievedTool.Should().NotBeNull();
        retrievedTool!.Name.Should().Be("test_tool");
    }

    [Fact]
    public void RegisterTool_WithDuplicateName_ShouldNotOverwrite()
    {
        // Arrange
        var tool1 = CreateMockTool("test_tool");
        var tool2 = CreateMockTool("test_tool");

        // Act
        _registry.RegisterTool(tool1);
        _registry.RegisterTool(tool2);

        // Assert
        _registry.Count.Should().Be(1);
    }

    [Fact]
    public void GetTool_WithUnknownName_ShouldReturnNull()
    {
        // Act
        var result = _registry.GetTool("unknown_tool");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ListTools_ShouldReturnAllTools()
    {
        // Arrange
        _registry.RegisterTool(CreateMockTool("tool1"));
        _registry.RegisterTool(CreateMockTool("tool2"));
        _registry.RegisterTool(CreateMockTool("tool3"));

        // Act
        var tools = _registry.ListTools();

        // Assert
        tools.Should().HaveCount(3);
    }

    [Fact]
    public void ListTools_WithCategory_ShouldFilterByCategory()
    {
        // Arrange
        _registry.RegisterTool(CreateMockTool("file_tool", ToolCategory.FileOperations));
        _registry.RegisterTool(CreateMockTool("git_tool", ToolCategory.GitOperations));
        _registry.RegisterTool(CreateMockTool("file_tool2", ToolCategory.FileOperations));

        // Act
        var fileTools = _registry.ListTools(ToolCategory.FileOperations);

        // Assert
        fileTools.Should().HaveCount(2);
        fileTools.All(t => t.Category == ToolCategory.FileOperations).Should().BeTrue();
    }

    [Fact]
    public void UnregisterTool_ShouldRemoveToolFromRegistry()
    {
        // Arrange
        var tool = CreateMockTool("test_tool");
        _registry.RegisterTool(tool);

        // Act
        var result = _registry.UnregisterTool("test_tool");

        // Assert
        result.Should().BeTrue();
        _registry.Count.Should().Be(0);
        _registry.GetTool("test_tool").Should().BeNull();
    }

    [Fact]
    public void GetToolSchemas_ShouldReturnValidSchemas()
    {
        // Arrange
        _registry.RegisterTool(CreateMockTool("tool1"));
        _registry.RegisterTool(CreateMockTool("tool2"));

        // Act
        var schemas = _registry.GetToolSchemas();

        // Assert
        schemas.Should().HaveCount(2);
        schemas.All(s => !string.IsNullOrWhiteSpace(s.Name)).Should().BeTrue();
        schemas.All(s => !string.IsNullOrWhiteSpace(s.Description)).Should().BeTrue();
    }

    [Fact]
    public async Task ValidateToolAsync_WithValidTool_ShouldReturnTrue()
    {
        // Arrange
        var tool = CreateMockTool("valid_tool");

        // Act
        var result = await _registry.ValidateToolAsync(tool);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Clear_ShouldRemoveAllTools()
    {
        // Arrange
        _registry.RegisterTool(CreateMockTool("tool1"));
        _registry.RegisterTool(CreateMockTool("tool2"));

        // Act
        _registry.Clear();

        // Assert
        _registry.Count.Should().Be(0);
    }

    private ITool CreateMockTool(string name, ToolCategory category = ToolCategory.FileOperations)
    {
        var mockTool = new Mock<ITool>();
        mockTool.Setup(t => t.Name).Returns(name);
        mockTool.Setup(t => t.Description).Returns($"Description for {name}");
        mockTool.Setup(t => t.Category).Returns(category);
        mockTool.Setup(t => t.ParametersSchema).Returns(@"{""type"": ""object"", ""properties"": {}}");
        mockTool.Setup(t => t.RequiresConfirmation).Returns(false);
        mockTool.Setup(t => t.DangerLevel).Returns(DangerLevel.Safe);
        mockTool.Setup(t => t.ValidateArgsAsync(It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockTool.Setup(t => t.GetExamples()).Returns(new List<string> { "example1" });

        return mockTool.Object;
    }
}
