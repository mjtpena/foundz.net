using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Foundz.Net.Tools.Testing;

/// <summary>
/// Tool for generating unit tests for code.
/// </summary>
public class GenerateTestTool : ITool
{
    public string Name => "generate_test";
    public string Description => "Generate unit test scaffolding for a class or method using appropriate test framework.";
    public ToolCategory Category => ToolCategory.Testing;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "filePath": {
                    "type": "string",
                    "description": "Path to the file containing code to test"
                },
                "className": {
                    "type": "string",
                    "description": "Name of the class to generate tests for"
                },
                "methodName": {
                    "type": "string",
                    "description": "Optional: Specific method to generate test for"
                },
                "testFramework": {
                    "type": "string",
                    "description": "Test framework to use (xUnit, NUnit, MSTest)",
                    "enum": ["xUnit", "NUnit", "MSTest"],
                    "default": "xUnit"
                }
            },
            "required": ["filePath", "className"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("filePath") || args["filePath"] is not string filePath || string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult(false);

        if (!args.ContainsKey("className") || args["className"] is not string className || string.IsNullOrWhiteSpace(className))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var filePath = args["filePath"].ToString()!;
            var className = args["className"].ToString()!;
            var methodName = args.ContainsKey("methodName") ? args["methodName"].ToString() : null;
            var testFramework = args.ContainsKey("testFramework") ? args["testFramework"].ToString() : "xUnit";

            if (!System.IO.File.Exists(filePath))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"File not found: {filePath}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var code = await System.IO.File.ReadAllTextAsync(filePath, cancellationToken);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync(cancellationToken);

            var classDeclaration = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == className);

            if (classDeclaration == null)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Class not found: {className}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var methods = classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(m => !string.IsNullOrEmpty(methodName) ? m.Identifier.Text == methodName : true)
                .ToList();

            var testCode = GenerateTestClass(className, methods, testFramework!);

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = $"Generated test class for {className}:\n\n{testCode}",
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["className"] = className,
                    ["methodCount"] = methods.Count,
                    ["testFramework"] = testFramework!
                }
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = false,
                Error = ex.Message,
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    private static string GenerateTestClass(string className, List<MethodDeclarationSyntax> methods, string testFramework)
    {
        var sb = new StringBuilder();

        // Add usings based on framework
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading.Tasks;");
        
        switch (testFramework)
        {
            case "xUnit":
                sb.AppendLine("using Xunit;");
                break;
            case "NUnit":
                sb.AppendLine("using NUnit.Framework;");
                break;
            case "MSTest":
                sb.AppendLine("using Microsoft.VisualStudio.TestTools.UnitTesting;");
                break;
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {className}.Tests;");
        sb.AppendLine();

        // Add class attribute for MSTest
        if (testFramework == "MSTest")
        {
            sb.AppendLine("[TestClass]");
        }

        sb.AppendLine($"public class {className}Tests");
        sb.AppendLine("{");

        // Generate setup method
        if (testFramework == "NUnit" || testFramework == "MSTest")
        {
            var setupAttr = testFramework == "NUnit" ? "[SetUp]" : "[TestInitialize]";
            sb.AppendLine($"    {setupAttr}");
            sb.AppendLine("    public void Setup()");
            sb.AppendLine("    {");
            sb.AppendLine($"        // Initialize test dependencies");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Generate test methods
        foreach (var method in methods)
        {
            if (method.Modifiers.Any(m => m.Text == "private"))
                continue; // Skip private methods

            var methodName = method.Identifier.Text;
            var isAsync = method.Modifiers.Any(m => m.Text == "async");

            // Test attribute based on framework
            var testAttr = testFramework switch
            {
                "xUnit" => "[Fact]",
                "NUnit" => "[Test]",
                "MSTest" => "[TestMethod]",
                _ => "[Fact]"
            };

            sb.AppendLine($"    {testAttr}");
            sb.AppendLine($"    public {(isAsync ? "async Task" : "void")} {methodName}_ShouldReturnExpectedResult()");
            sb.AppendLine("    {");
            sb.AppendLine("        // Arrange");
            sb.AppendLine($"        var sut = new {className}();");
            sb.AppendLine("        // TODO: Setup test data");
            sb.AppendLine();
            sb.AppendLine("        // Act");
            sb.AppendLine($"        // TODO: Call {methodName}");
            sb.AppendLine();
            sb.AppendLine("        // Assert");
            sb.AppendLine("        // TODO: Verify results");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "generate_test({\"filePath\": \"Calculator.cs\", \"className\": \"Calculator\"})",
            "generate_test({\"filePath\": \"Service.cs\", \"className\": \"UserService\", \"testFramework\": \"xUnit\"})"
        ];
    }
}
