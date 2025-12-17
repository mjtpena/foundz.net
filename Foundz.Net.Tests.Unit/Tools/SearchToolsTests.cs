using Foundz.Net.Tools.Search;
using Foundz.Net.Shared.Interfaces;
using Xunit;
using FluentAssertions;

namespace Foundz.Net.Tests.Unit.Tools;

public class SearchToolsTests
{
    [Fact]
    public void SearchCodebaseTool_Properties_ShouldBeCorrect()
    {
        var tool = new SearchCodebaseTool();
        tool.Name.Should().Be("search_codebase");
        tool.Category.Should().Be(ToolCategory.Search);
        tool.DangerLevel.Should().Be(DangerLevel.Safe);
    }

    [Fact]
    public void SearchDependenciesTool_Properties_ShouldBeCorrect()
    {
        var tool = new SearchDependenciesTool();
        tool.Name.Should().Be("search_dependencies");
        tool.Category.Should().Be(ToolCategory.Search);
    }

    [Fact]
    public void SearchDocumentationTool_Properties_ShouldBeCorrect()
    {
        var tool = new SearchDocumentationTool();
        tool.Name.Should().Be("search_documentation");
        tool.Category.Should().Be(ToolCategory.Search);
    }

    [Fact]
    public async Task SearchCodebaseTool_ValidateArgs_ShouldRequireQuery()
    {
        var tool = new SearchCodebaseTool();
        var result = await tool.ValidateArgsAsync(new Dictionary<string, object> { ["query"] = "test" });
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SearchDependenciesTool_ValidateArgs_ShouldAcceptPath()
    {
        var tool = new SearchDependenciesTool();
        var result = await tool.ValidateArgsAsync(new Dictionary<string, object> { ["path"] = "." });
        result.Should().BeTrue();
    }

    [Fact]
    public void SearchCodebaseTool_GetExamples_ShouldReturnExamples()
    {
        var tool = new SearchCodebaseTool();
        var examples = tool.GetExamples();
        examples.Should().NotBeEmpty();
    }

    [Fact]
    public void SearchDependenciesTool_GetExamples_ShouldReturnExamples()
    {
        var tool = new SearchDependenciesTool();
        var examples = tool.GetExamples();
        examples.Should().NotBeEmpty();
    }
}
