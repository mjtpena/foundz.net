using Foundz.Net.Tools.Search;
using Foundz.Net.Tools.Testing;
using Foundz.Net.Tools.Documentation;
using Foundz.Net.Shared.Interfaces;
using Xunit;
using FluentAssertions;

namespace Foundz.Net.Tests.Unit.Tools;

public class SearchAndTestingToolsTests
{
    #region Search Tools

    [Fact]
    public void SearchCodebaseTool_ShouldHaveCorrectProperties()
    {
        var tool = new SearchCodebaseTool();
        tool.Name.Should().Be("search_codebase");
        tool.Category.Should().Be(ToolCategory.Search);
        tool.DangerLevel.Should().Be(DangerLevel.Safe);
    }

    [Fact]
    public async Task SearchCodebaseTool_ShouldValidateArgs()
    {
        var tool = new SearchCodebaseTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["query"] = "test",
            ["path"] = "."
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void SearchDependenciesTool_ShouldHaveCorrectProperties()
    {
        var tool = new SearchDependenciesTool();
        tool.Name.Should().Be("search_dependencies");
        tool.RequiresConfirmation.Should().BeFalse();
    }

    [Fact]
    public async Task SearchDependenciesTool_ShouldValidateArgs()
    {
        var tool = new SearchDependenciesTool();
        // SearchDependenciesTool has no required args, always valid
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["packageName"] = "Newtonsoft.Json",
            ["projectPath"] = "test.csproj"
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeTrue();
    }

    [Fact]
    public void SearchDocumentationTool_ShouldHaveCorrectProperties()
    {
        var tool = new SearchDocumentationTool();
        tool.Name.Should().Be("search_documentation");
        tool.DangerLevel.Should().Be(DangerLevel.Safe);
    }

    [Fact]
    public async Task SearchDocumentationTool_ShouldValidateArgs()
    {
        var tool = new SearchDocumentationTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["query"] = "configuration",
            ["path"] = "."
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void AllSearchTools_ShouldProvideExamples()
    {
        var tools = new ITool[]
        {
            new SearchCodebaseTool(),
            new SearchDependenciesTool(),
            new SearchDocumentationTool()
        };

        foreach (var tool in tools)
        {
            var examples = tool.GetExamples();
            examples.Should().NotBeEmpty($"{tool.Name} should provide examples");
        }
    }

    #endregion

    #region Testing Tools

    [Fact]
    public void GenerateTestTool_ShouldHaveCorrectProperties()
    {
        var tool = new GenerateTestTool();
        tool.Name.Should().Be("generate_test");
        tool.Category.Should().Be(ToolCategory.Testing);
        tool.RequiresConfirmation.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateTestTool_ShouldValidateArgs()
    {
        var tool = new GenerateTestTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["className"] = "MyClass",
            ["filePath"] = "test.cs"
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void AnalyzeCoverageTool_ShouldHaveCorrectProperties()
    {
        var tool = new AnalyzeCoverageTool();
        tool.Name.Should().Be("analyze_coverage");
        tool.Category.Should().Be(ToolCategory.Testing);
        tool.DangerLevel.Should().Be(DangerLevel.Safe);
    }

    [Fact]
    public async Task AnalyzeCoverageTool_ShouldValidateArgs()
    {
        var tool = new AnalyzeCoverageTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["coverageFile"] = "coverage.xml"
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void AllTestingTools_ShouldProvideExamples()
    {
        var tools = new ITool[]
        {
            new GenerateTestTool(),
            new AnalyzeCoverageTool()
        };

        foreach (var tool in tools)
        {
            var examples = tool.GetExamples();
            examples.Should().NotBeEmpty($"{tool.Name} should provide examples");
        }
    }

    #endregion

    #region Documentation Tools

    [Fact]
    public void GenerateDocsTool_ShouldHaveCorrectProperties()
    {
        var tool = new GenerateDocsTool();
        tool.Name.Should().Be("generate_docs");
        tool.Category.Should().Be(ToolCategory.Documentation);
        tool.RequiresConfirmation.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateDocsTool_ShouldValidateArgs()
    {
        var tool = new GenerateDocsTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["filePath"] = "src/test.cs"
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void ExplainCodeTool_ShouldHaveCorrectProperties()
    {
        var tool = new ExplainCodeTool();
        tool.Name.Should().Be("explain_code");
        tool.Category.Should().Be(ToolCategory.Documentation);
        tool.DangerLevel.Should().Be(DangerLevel.Safe);
    }

    [Fact]
    public async Task ExplainCodeTool_ShouldValidateArgs()
    {
        var tool = new ExplainCodeTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["filePath"] = "test.cs"
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void AllDocumentationTools_ShouldProvideExamples()
    {
        var tools = new ITool[]
        {
            new GenerateDocsTool(),
            new ExplainCodeTool()
        };

        foreach (var tool in tools)
        {
            var examples = tool.GetExamples();
            examples.Should().NotBeEmpty($"{tool.Name} should provide examples");
        }
    }

    #endregion
}
