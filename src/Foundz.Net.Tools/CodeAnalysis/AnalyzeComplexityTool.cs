using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Text;

namespace Foundz.Net.Tools.CodeAnalysis;

/// <summary>
/// Tool for analyzing code complexity metrics.
/// </summary>
public class AnalyzeComplexityTool : ITool
{
    public string Name => "analyze_complexity";
    public string Description => "Analyze cyclomatic complexity of C# code and identify complex methods";
    public ToolCategory Category => ToolCategory.CodeAnalysis;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "file_path": {
                    "type": "string",
                    "description": "Path to C# source file"
                },
                "threshold": {
                    "type": "integer",
                    "description": "Complexity threshold for warnings (default: 10)"
                }
            },
            "required": ["file_path"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(args.ContainsKey("file_path"));
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var filePath = args["file_path"].ToString()!;
            var threshold = args.ContainsKey("threshold") ? Convert.ToInt32(args["threshold"]) : 10;

            if (!System.IO.File.Exists(filePath))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"File not found: {filePath}",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            var code = await System.IO.File.ReadAllTextAsync(filePath, cancellationToken);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync(cancellationToken);

            var output = new StringBuilder();
            output.AppendLine($"Complexity Analysis: {Path.GetFileName(filePath)}");
            output.AppendLine($"Threshold: {threshold}");
            output.AppendLine();

            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            var complexityData = new List<(string methodName, int complexity, int lineCount)>();

            foreach (var method in methods)
            {
                var complexity = CalculateCyclomaticComplexity(method);
                var lineCount = method.GetLocation().GetLineSpan().EndLinePosition.Line - 
                               method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                complexityData.Add((method.Identifier.Text, complexity, lineCount));
            }

            // Sort by complexity (highest first)
            complexityData = complexityData.OrderByDescending(d => d.complexity).ToList();

            // Summary statistics
            if (complexityData.Any())
            {
                var avgComplexity = complexityData.Average(d => d.complexity);
                var maxComplexity = complexityData.Max(d => d.complexity);
                var highComplexityCount = complexityData.Count(d => d.complexity > threshold);

                output.AppendLine("SUMMARY:");
                output.AppendLine($"  Total Methods: {complexityData.Count}");
                output.AppendLine($"  Average Complexity: {avgComplexity:F1}");
                output.AppendLine($"  Max Complexity: {maxComplexity}");
                output.AppendLine($"  Methods Above Threshold: {highComplexityCount}");
                output.AppendLine();

                // High complexity methods
                var highComplexity = complexityData.Where(d => d.complexity > threshold).ToList();
                if (highComplexity.Any())
                {
                    output.AppendLine("HIGH COMPLEXITY METHODS:");
                    foreach (var (methodName, complexity, lineCount) in highComplexity)
                    {
                        var warning = complexity > threshold * 2 ? "⚠️ CRITICAL" : "⚠️ WARNING";
                        output.AppendLine($"  {warning} {methodName}");
                        output.AppendLine($"    Complexity: {complexity}");
                        output.AppendLine($"    Lines: {lineCount}");
                        output.AppendLine($"    Suggestion: Consider refactoring into smaller methods");
                        output.AppendLine();
                    }
                }

                // All methods detail
                output.AppendLine("ALL METHODS:");
                output.AppendLine("┌─────────────────────────────┬────────────┬───────┐");
                output.AppendLine("│ Method                      │ Complexity │ Lines │");
                output.AppendLine("├─────────────────────────────┼────────────┼───────┤");
                
                foreach (var (methodName, complexity, lineCount) in complexityData)
                {
                    var status = complexity > threshold ? "🔴" : complexity > threshold / 2 ? "🟡" : "🟢";
                    var nameDisplay = methodName.Length > 27 ? methodName.Substring(0, 24) + "..." : methodName.PadRight(27);
                    output.AppendLine($"│ {status} {nameDisplay} │ {complexity,10} │ {lineCount,5} │");
                }
                
                output.AppendLine("└─────────────────────────────┴────────────┴───────┘");
            }
            else
            {
                output.AppendLine("No methods found in file.");
            }

            stopwatch.Stop();

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = output.ToString(),
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = false,
                Error = $"Error analyzing complexity: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    private int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
    {
        // Start with 1 for the method itself
        int complexity = 1;

        // Count decision points
        var body = method.Body;
        if (body == null)
            return complexity;

        // If statements
        complexity += body.DescendantNodes().OfType<IfStatementSyntax>().Count();

        // While loops
        complexity += body.DescendantNodes().OfType<WhileStatementSyntax>().Count();

        // For loops
        complexity += body.DescendantNodes().OfType<ForStatementSyntax>().Count();

        // Foreach loops
        complexity += body.DescendantNodes().OfType<ForEachStatementSyntax>().Count();

        // Case statements (count each case)
        complexity += body.DescendantNodes().OfType<SwitchSectionSyntax>().Count();

        // Catch clauses
        complexity += body.DescendantNodes().OfType<CatchClauseSyntax>().Count();

        // Conditional expressions (ternary operators)
        complexity += body.DescendantNodes().OfType<ConditionalExpressionSyntax>().Count();

        // Logical AND/OR operators
        var binaryExpressions = body.DescendantNodes().OfType<BinaryExpressionSyntax>();
        complexity += binaryExpressions.Count(e => 
            e.IsKind(SyntaxKind.LogicalAndExpression) || 
            e.IsKind(SyntaxKind.LogicalOrExpression));

        // Null coalescing operators
        complexity += binaryExpressions.Count(e => e.IsKind(SyntaxKind.CoalesceExpression));

        return complexity;
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """analyze_complexity({"file_path": "Program.cs"})""",
            """analyze_complexity({"file_path": "ComplexService.cs", "threshold": 15})"""
        };
    }
}
