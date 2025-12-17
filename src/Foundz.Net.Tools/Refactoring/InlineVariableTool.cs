using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace Foundz.Net.Tools.Refactoring;

/// <summary>
/// Tool for inlining variable declarations by replacing usages with the initializer expression.
/// Uses Roslyn for advanced semantic analysis and safe refactoring.
/// </summary>
public class InlineVariableTool : ITool
{
    public string Name => "inline_variable";
    public string Description => "Inline a variable by replacing all its usages with its initializer expression. Validates safety and preserves semantics.";
    public ToolCategory Category => ToolCategory.Refactoring;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Warning;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "filePath": {
                    "type": "string",
                    "description": "Path to the C# source file"
                },
                "variableName": {
                    "type": "string",
                    "description": "Name of the variable to inline"
                },
                "lineNumber": {
                    "type": "integer",
                    "description": "Line number where the variable is declared (1-indexed)"
                },
                "dryRun": {
                    "type": "boolean",
                    "description": "If true, show changes without applying them",
                    "default": false
                }
            },
            "required": ["filePath", "variableName"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("filePath") || args["filePath"] is not string path || string.IsNullOrWhiteSpace(path))
            return Task.FromResult(false);

        if (!args.ContainsKey("variableName") || args["variableName"] is not string name || string.IsNullOrWhiteSpace(name))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = (string)args["filePath"];
            var variableName = (string)args["variableName"];
            var dryRun = args.GetValueOrDefault("dryRun") as bool? ?? false;
            var lineNumber = args.GetValueOrDefault("lineNumber") as int?;

            if (!System.IO.File.Exists(filePath))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"File not found: {filePath}"
                };
            }

            var code = await System.IO.File.ReadAllTextAsync(filePath, cancellationToken);
            var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
            var root = await tree.GetRootAsync(cancellationToken);

            // Find the variable declaration
            var declarations = root.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .Where(v => v.Identifier.Text == variableName);

            if (lineNumber.HasValue)
            {
                var line = lineNumber.Value;
                declarations = declarations.Where(v =>
                {
                    var span = v.GetLocation().GetLineSpan();
                    return span.StartLinePosition.Line + 1 == line;
                });
            }

            var declarator = declarations.FirstOrDefault();
            if (declarator == null)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Variable '{variableName}' not found at specified location"
                };
            }

            // Check if variable has an initializer
            if (declarator.Initializer == null)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Variable '{variableName}' has no initializer. Cannot inline."
                };
            }

            // Get the initializer expression
            var initializerExpression = declarator.Initializer.Value;
            
            // Find all references to this variable
            var identifiers = root.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(id => id.Identifier.Text == variableName)
                .Where(id => id.Identifier.Text != declarator.Identifier.Text)
                .ToList();

            if (identifiers.Count == 0)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = true,
                    Output = $"Variable '{variableName}' is never used. Consider removing it instead of inlining.",
                    Metadata = new Dictionary<string, object>
                    {
                        ["usageCount"] = 0,
                        ["recommendation"] = "DELETE"
                    }
                };
            }

            // Check for safety: variable should not be reassigned
            var parent = declarator.Parent;
            while (parent != null && parent is not MethodDeclarationSyntax and not PropertyDeclarationSyntax)
            {
                parent = parent.Parent;
            }

            if (parent != null)
            {
                var assignments = parent.DescendantNodes()
                    .OfType<AssignmentExpressionSyntax>()
                    .Where(a => a.Left is IdentifierNameSyntax id && id.Identifier.Text == variableName)
                    .ToList();

                if (assignments.Any())
                {
                    return new ToolResult
                    {
                        ToolCallId = string.Empty,
                        ToolName = Name,
                        Success = false,
                        Error = $"Cannot inline variable '{variableName}': it is reassigned after declaration. " +
                                $"Inlining is only safe for variables that are not modified."
                    };
                }
            }

            // Build preview/changes
            var changes = new List<string>();
            var modifiedRoot = root;

            // Replace all usages with the initializer expression
            foreach (var identifier in identifiers.OrderByDescending(id => id.SpanStart))
            {
                var location = identifier.GetLocation().GetLineSpan();
                changes.Add($"Line {location.StartLinePosition.Line + 1}: Replace '{variableName}' with '{initializerExpression}'");
                
                modifiedRoot = modifiedRoot.ReplaceNode(
                    modifiedRoot.FindNode(identifier.Span),
                    initializerExpression);
            }

            // Remove the variable declaration
            var declarationLocation = declarator.GetLocation().GetLineSpan();
            changes.Add($"Line {declarationLocation.StartLinePosition.Line + 1}: Remove variable declaration");
            
            var declaration = declarator.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
            if (declaration != null)
            {
                modifiedRoot = modifiedRoot.RemoveNode(
                    modifiedRoot.FindNode(declaration.Span),
                    SyntaxRemoveOptions.KeepNoTrivia)!;
            }

            var modifiedCode = modifiedRoot.ToFullString();

            if (dryRun)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = true,
                    Output = $"Dry run: Would inline variable '{variableName}' ({identifiers.Count} usage(s))",
                    Metadata = new Dictionary<string, object>
                    {
                        ["changes"] = changes,
                        ["usageCount"] = identifiers.Count,
                        ["preview"] = modifiedCode,
                        ["dryRun"] = true
                    }
                };
            }

            // Apply changes
            await System.IO.File.WriteAllTextAsync(filePath, modifiedCode, cancellationToken);

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = $"Successfully inlined variable '{variableName}' ({identifiers.Count} usage(s) replaced)",
                Metadata = new Dictionary<string, object>
                {
                    ["changes"] = changes,
                    ["usageCount"] = identifiers.Count,
                    ["filePath"] = filePath
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
                Error = $"Failed to inline variable: {ex.Message}"
            };
        }
    }

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "inline_variable({\"filePath\": \"src/Calculator.cs\", \"variableName\": \"temp\", \"dryRun\": true})",
            "inline_variable({\"filePath\": \"Program.cs\", \"variableName\": \"result\", \"lineNumber\": 42})"
        ];
    }
}
