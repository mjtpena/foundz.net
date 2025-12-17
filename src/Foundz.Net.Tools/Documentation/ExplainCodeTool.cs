using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Foundz.Net.Tools.Documentation;

/// <summary>
/// Tool for generating natural language explanations of code.
/// </summary>
public class ExplainCodeTool : ITool
{
    public string Name => "explain_code";
    public string Description => "Generate natural language explanation of code logic and flow.";
    public ToolCategory Category => ToolCategory.Documentation;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "filePath": {
                    "type": "string",
                    "description": "Path to the file containing code to explain"
                },
                "startLine": {
                    "type": "integer",
                    "description": "Optional: Starting line number (1-indexed)"
                },
                "endLine": {
                    "type": "integer",
                    "description": "Optional: Ending line number (1-indexed)"
                },
                "detailLevel": {
                    "type": "string",
                    "description": "Level of detail (brief, normal, detailed)",
                    "enum": ["brief", "normal", "detailed"],
                    "default": "normal"
                }
            },
            "required": ["filePath"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("filePath") || args["filePath"] is not string path || string.IsNullOrWhiteSpace(path))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var filePath = args["filePath"].ToString()!;
            var startLine = args.ContainsKey("startLine") ? Convert.ToInt32(args["startLine"]) : (int?)null;
            var endLine = args.ContainsKey("endLine") ? Convert.ToInt32(args["endLine"]) : (int?)null;
            var detailLevel = args.ContainsKey("detailLevel") ? args["detailLevel"].ToString() : "normal";

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
            var lines = code.Split('\n');

            // If specific lines requested, extract them
            if (startLine.HasValue && endLine.HasValue)
            {
                if (startLine.Value < 1 || endLine.Value > lines.Length)
                {
                    return new ToolResult
                    {
                        ToolCallId = string.Empty,
                        ToolName = Name,
                        Success = false,
                        Error = "Invalid line range",
                        ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                    };
                }

                code = string.Join('\n', lines[(startLine.Value - 1)..endLine.Value]);
            }

            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync(cancellationToken);

            var output = new StringBuilder();
            output.AppendLine($"Code Explanation: {Path.GetFileName(filePath)}");
            
            if (startLine.HasValue && endLine.HasValue)
            {
                output.AppendLine($"Lines: {startLine}-{endLine}");
            }
            
            output.AppendLine($"Detail Level: {detailLevel}");
            output.AppendLine();

            // Analyze the code structure
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
            var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();

            if (classes.Any())
            {
                output.AppendLine("📦 Classes:");
                foreach (var cls in classes)
                {
                    output.AppendLine($"  - {cls.Identifier.Text}");
                    
                    if (detailLevel == "detailed")
                    {
                        var baseTypes = cls.BaseList?.Types.Select(t => t.ToString()) ?? [];
                        if (baseTypes.Any())
                        {
                            output.AppendLine($"    Inherits/Implements: {string.Join(", ", baseTypes)}");
                        }
                    }
                }
                output.AppendLine();
            }

            if (methods.Any())
            {
                output.AppendLine("🔧 Methods:");
                foreach (var method in methods)
                {
                    var modifierToken = method.Modifiers.FirstOrDefault(m => 
                        m.Text == "public" || m.Text == "private" || m.Text == "protected");
                    var accessibility = modifierToken.Text != null ? modifierToken.Text : "internal";
                    
                    output.AppendLine($"  - {accessibility} {method.ReturnType} {method.Identifier.Text}(...)");

                    if (detailLevel != "brief")
                    {
                        var parameters = method.ParameterList.Parameters;
                        if (parameters.Any())
                        {
                            output.AppendLine($"    Parameters: {string.Join(", ", parameters.Select(p => $"{p.Type} {p.Identifier}"))}");
                        }

                        // Analyze method body complexity
                        var statements = method.Body?.Statements.Count ?? 0;
                        var loops = method.DescendantNodes().OfType<ForStatementSyntax>().Count() +
                                   method.DescendantNodes().OfType<WhileStatementSyntax>().Count() +
                                   method.DescendantNodes().OfType<ForEachStatementSyntax>().Count();
                        var conditionals = method.DescendantNodes().OfType<IfStatementSyntax>().Count();

                        output.AppendLine($"    Complexity: {statements} statements, {loops} loops, {conditionals} conditionals");
                    }
                }
                output.AppendLine();
            }

            if (properties.Any() && detailLevel != "brief")
            {
                output.AppendLine("📋 Properties:");
                foreach (var prop in properties)
                {
                    var hasGetter = prop.AccessorList?.Accessors.Any(a => a.Kind() == SyntaxKind.GetAccessorDeclaration) ?? false;
                    var hasSetter = prop.AccessorList?.Accessors.Any(a => a.Kind() == SyntaxKind.SetAccessorDeclaration) ?? false;
                    var access = hasGetter && hasSetter ? "get/set" : hasGetter ? "get only" : "set only";
                    
                    output.AppendLine($"  - {prop.Type} {prop.Identifier.Text} ({access})");
                }
                output.AppendLine();
            }

            output.AppendLine("📝 Summary:");
            output.AppendLine($"This code defines {classes.Count} class(es) with {methods.Count} method(s) and {properties.Count} property(ies).");
            
            if (detailLevel == "detailed")
            {
                var totalLines = lines.Length;
                var commentLines = lines.Count(l => l.TrimStart().StartsWith("//") || l.TrimStart().StartsWith("/*"));
                output.AppendLine($"Total lines: {totalLines} ({commentLines} comment lines)");
            }

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = output.ToString(),
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["filePath"] = filePath,
                    ["classCount"] = classes.Count,
                    ["methodCount"] = methods.Count,
                    ["propertyCount"] = properties.Count
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

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "explain_code({\"filePath\": \"Calculator.cs\"})",
            "explain_code({\"filePath\": \"Service.cs\", \"startLine\": 45, \"endLine\": 60, \"detailLevel\": \"detailed\"})"
        ];
    }
}
