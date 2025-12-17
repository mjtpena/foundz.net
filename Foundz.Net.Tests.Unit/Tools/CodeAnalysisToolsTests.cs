using Foundz.Net.Tools.CodeAnalysis;
using Foundz.Net.Shared.Interfaces;
using Xunit;
using FluentAssertions;

namespace Foundz.Net.Tests.Unit.Tools;

public class CodeAnalysisToolsTests : IDisposable
{
    private readonly string _testDirectory;

    public CodeAnalysisToolsTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "foundz_code_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, true); } catch { }
        }
    }

    [Fact]
    public void ParseCodeTool_ShouldHaveCorrectProperties()
    {
        var tool = new ParseCodeTool();
        tool.Name.Should().Be("parse_code");
        tool.Category.Should().Be(ToolCategory.CodeAnalysis);
    }

    [Fact]
    public async Task ParseCodeTool_ShouldValidateArgs()
    {
        var tool = new ParseCodeTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> { ["file_path"] = "test.cs" })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public async Task ParseCodeTool_ShouldParseSimpleCode()
    {
        var tool = new ParseCodeTool();
        var args = new Dictionary<string, object>
        {
            ["code"] = "public class Test { }",
            ["language"] = "csharp"
        };

        var result = await tool.ExecuteAsync(args);
        result.Should().NotBeNull();
    }

    [Fact]
    public void LintCodeTool_ShouldHaveCorrectProperties()
    {
        var tool = new LintCodeTool();
        tool.Name.Should().Be("lint_code");
        tool.RequiresConfirmation.Should().BeFalse();
    }

    [Fact]
    public async Task LintCodeTool_ShouldValidateArgs()
    {
        var tool = new LintCodeTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> { ["projectPath"] = "test.csproj" })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void GetDefinitionTool_ShouldHaveCorrectProperties()
    {
        var tool = new GetDefinitionTool();
        tool.Name.Should().Be("get_definition");
        tool.DangerLevel.Should().Be(DangerLevel.Safe);
    }

    [Fact]
    public async Task GetDefinitionTool_ShouldValidateArgs()
    {
        var tool = new GetDefinitionTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["symbolName"] = "Test",
            ["projectPath"] = "test.csproj"
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void FindReferencesTool_ShouldHaveCorrectProperties()
    {
        var tool = new FindReferencesTool();
        tool.Name.Should().Be("find_references");
        tool.Category.Should().Be(ToolCategory.CodeAnalysis);
    }

    [Fact]
    public async Task FindReferencesTool_ShouldValidateArgs()
    {
        var tool = new FindReferencesTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["symbolName"] = "Test",
            ["projectPath"] = "test.csproj"
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void DetectDuplicationTool_ShouldHaveCorrectProperties()
    {
        var tool = new DetectDuplicationTool();
        tool.Name.Should().Be("detect_duplication");
        tool.RequiresConfirmation.Should().BeFalse();
    }

    [Fact]
    public async Task DetectDuplicationTool_ShouldValidateArgs()
    {
        var tool = new DetectDuplicationTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> { ["directoryPath"] = "." })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void AnalyzeComplexityTool_ShouldHaveCorrectProperties()
    {
        var tool = new AnalyzeComplexityTool();
        tool.Name.Should().Be("analyze_complexity");
        tool.DangerLevel.Should().Be(DangerLevel.Safe);
    }

    [Fact]
    public async Task AnalyzeComplexityTool_ShouldValidateArgs()
    {
        var tool = new AnalyzeComplexityTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> { ["file_path"] = "test.cs" })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void AllCodeAnalysisTools_ShouldProvideExamples()
    {
        var tools = new ITool[]
        {
            new ParseCodeTool(),
            new LintCodeTool(),
            new GetDefinitionTool(),
            new FindReferencesTool(),
            new DetectDuplicationTool(),
            new AnalyzeComplexityTool()
        };

        foreach (var tool in tools)
        {
            var examples = tool.GetExamples();
            examples.Should().NotBeEmpty($"{tool.Name} should provide examples");
        }
    }
}
