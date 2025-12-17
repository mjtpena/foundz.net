using Foundz.Net.Tools.Refactoring;
using Foundz.Net.Shared.Interfaces;
using Xunit;
using FluentAssertions;

namespace Foundz.Net.Tests.Unit.Tools;

public class RefactoringToolsTests
{
    [Fact]
    public void RenameSymbolTool_ShouldHaveCorrectProperties()
    {
        var tool = new RenameSymbolTool();
        tool.Name.Should().Be("rename_symbol");
        tool.Category.Should().Be(ToolCategory.Refactoring);
        tool.RequiresConfirmation.Should().BeTrue();
        tool.DangerLevel.Should().Be(DangerLevel.Warning);
    }

    [Fact]
    public async Task RenameSymbolTool_ShouldValidateArgs()
    {
        var tool = new RenameSymbolTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["oldName"] = "OldClass",
            ["newName"] = "NewClass",
            ["projectPath"] = "test.csproj"
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void ExtractMethodTool_ShouldHaveCorrectProperties()
    {
        var tool = new ExtractMethodTool();
        tool.Name.Should().Be("extract_method");
        tool.Category.Should().Be(ToolCategory.Refactoring);
        tool.RequiresConfirmation.Should().BeTrue();
    }

    [Fact]
    public async Task ExtractMethodTool_ShouldValidateArgs()
    {
        var tool = new ExtractMethodTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["filePath"] = "test.cs",
            ["startLine"] = 10,
            ["endLine"] = 20,
            ["methodName"] = "NewMethod"
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void ExtractInterfaceTool_ShouldHaveCorrectProperties()
    {
        var tool = new ExtractInterfaceTool();
        tool.Name.Should().Be("extract_interface");
        tool.DangerLevel.Should().Be(DangerLevel.Warning);
    }

    [Fact]
    public async Task ExtractInterfaceTool_ShouldValidateArgs()
    {
        var tool = new ExtractInterfaceTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["className"] = "MyClass",
            ["interfaceName"] = "IMyClass",
            ["projectPath"] = "test.csproj"
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void InlineVariableTool_ShouldHaveCorrectProperties()
    {
        var tool = new InlineVariableTool();
        tool.Name.Should().Be("inline_variable");
        tool.RequiresConfirmation.Should().BeTrue();
    }

    [Fact]
    public async Task InlineVariableTool_ShouldValidateArgs()
    {
        var tool = new InlineVariableTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["variableName"] = "myVar",
            ["filePath"] = "test.cs",
            ["line"] = 42
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void MoveClassTool_ShouldHaveCorrectProperties()
    {
        var tool = new MoveClassTool();
        tool.Name.Should().Be("move_class");
        tool.Category.Should().Be(ToolCategory.Refactoring);
        tool.DangerLevel.Should().Be(DangerLevel.Warning);
    }

    [Fact]
    public async Task MoveClassTool_ShouldValidateArgs()
    {
        var tool = new MoveClassTool();
        (await tool.ValidateArgsAsync(new Dictionary<string, object> 
        { 
            ["className"] = "MyClass",
            ["sourceFile"] = "old.cs",
            ["targetFile"] = "new.cs"
        })).Should().BeTrue();
        (await tool.ValidateArgsAsync(new Dictionary<string, object>())).Should().BeFalse();
    }

    [Fact]
    public void AllRefactoringTools_ShouldProvideExamples()
    {
        var tools = new ITool[]
        {
            new RenameSymbolTool(),
            new ExtractMethodTool(),
            new ExtractInterfaceTool(),
            new InlineVariableTool(),
            new MoveClassTool()
        };

        foreach (var tool in tools)
        {
            var examples = tool.GetExamples();
            examples.Should().NotBeEmpty($"{tool.Name} should provide examples");
        }
    }

    [Fact]
    public void AllRefactoringTools_ShouldRequireConfirmation()
    {
        var tools = new ITool[]
        {
            new RenameSymbolTool(),
            new ExtractMethodTool(),
            new ExtractInterfaceTool(),
            new InlineVariableTool(),
            new MoveClassTool()
        };

        foreach (var tool in tools)
        {
            tool.RequiresConfirmation.Should().BeTrue($"{tool.Name} modifies code and should require confirmation");
        }
    }

    [Fact]
    public void AllRefactoringTools_ShouldHaveWarningLevel()
    {
        var tools = new ITool[]
        {
            new RenameSymbolTool(),
            new ExtractMethodTool(),
            new ExtractInterfaceTool(),
            new InlineVariableTool(),
            new MoveClassTool()
        };

        foreach (var tool in tools)
        {
            tool.DangerLevel.Should().Be(DangerLevel.Warning, $"{tool.Name} should have warning danger level");
        }
    }
}
