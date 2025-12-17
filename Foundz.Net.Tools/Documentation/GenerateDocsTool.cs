using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Foundz.Net.Tools.Documentation;

/// <summary>
/// Tool for generating documentation for code.
/// </summary>
public class GenerateDocsTool : ITool
{
    public string Name => "generate_docs";
    public string Description => "Generate XML documentation comments for classes, methods, and properties.";
    public ToolCategory Category => ToolCategory.Documentation;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "filePath": {
                    "type": "string",
                    "description": "Path to the file to generate docs for"
                },
                "memberName": {
                    "type": "string",
                    "description": "Optional: Specific member to document (documents all if not provided)"
                },
                "style": {
                    "type": "string",
                    "description": "Documentation style (XML, Markdown)",
                    "enum": ["XML", "Markdown"],
                    "default": "XML"
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
            var memberName = args.ContainsKey("memberName") ? args["memberName"].ToString() : null;
            var style = args.ContainsKey("style") ? args["style"].ToString() : "XML";

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

            var output = new StringBuilder();
            output.AppendLine($"Documentation for: {Path.GetFileName(filePath)}");
            output.AppendLine($"Style: {style}");
            output.AppendLine();

            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            var documentedCount = 0;

            foreach (var classDecl in classes)
            {
                if (!string.IsNullOrEmpty(memberName) && classDecl.Identifier.Text != memberName)
                    continue;

                output.AppendLine(GenerateClassDoc(classDecl, style!));
                documentedCount++;

                // Document methods
                foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
                {
                    if (string.IsNullOrEmpty(memberName) || method.Identifier.Text == memberName)
                    {
                        output.AppendLine(GenerateMethodDoc(method, style!));
                        documentedCount++;
                    }
                }

                // Document properties
                foreach (var property in classDecl.Members.OfType<PropertyDeclarationSyntax>())
                {
                    if (string.IsNullOrEmpty(memberName) || property.Identifier.Text == memberName)
                    {
                        output.AppendLine(GeneratePropertyDoc(property, style!));
                        documentedCount++;
                    }
                }
            }

            if (documentedCount == 0)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"No members found to document" + 
                           (string.IsNullOrEmpty(memberName) ? "" : $": {memberName}"),
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
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
                    ["documentedCount"] = documentedCount,
                    ["style"] = style!
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

    private static string GenerateClassDoc(ClassDeclarationSyntax classDecl, string style)
    {
        var sb = new StringBuilder();
        var className = classDecl.Identifier.Text;

        if (style == "XML")
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// Represents a {className}.");
            sb.AppendLine("/// </summary>");
        }
        else
        {
            sb.AppendLine($"## {className}");
            sb.AppendLine();
            sb.AppendLine($"Represents a {className}.");
        }

        sb.AppendLine($"public class {className}");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string GenerateMethodDoc(MethodDeclarationSyntax method, string style)
    {
        var sb = new StringBuilder();
        var methodName = method.Identifier.Text;
        var parameters = method.ParameterList.Parameters;

        if (style == "XML")
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// {methodName} method.");
            sb.AppendLine("/// </summary>");

            foreach (var param in parameters)
            {
                sb.AppendLine($"/// <param name=\"{param.Identifier.Text}\">The {param.Identifier.Text} parameter.</param>");
            }

            if (method.ReturnType.ToString() != "void")
            {
                sb.AppendLine($"/// <returns>The result of {methodName}.</returns>");
            }
        }
        else
        {
            sb.AppendLine($"### {methodName}");
            sb.AppendLine();
            sb.AppendLine($"{methodName} method.");
            sb.AppendLine();

            if (parameters.Any())
            {
                sb.AppendLine("**Parameters:**");
                foreach (var param in parameters)
                {
                    sb.AppendLine($"- `{param.Identifier.Text}` ({param.Type}): The {param.Identifier.Text} parameter.");
                }
                sb.AppendLine();
            }

            if (method.ReturnType.ToString() != "void")
            {
                sb.AppendLine($"**Returns:** {method.ReturnType}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string GeneratePropertyDoc(PropertyDeclarationSyntax property, string style)
    {
        var sb = new StringBuilder();
        var propertyName = property.Identifier.Text;

        if (style == "XML")
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// Gets or sets the {propertyName}.");
            sb.AppendLine("/// </summary>");
        }
        else
        {
            sb.AppendLine($"### {propertyName}");
            sb.AppendLine();
            sb.AppendLine($"Gets or sets the {propertyName}.");
            sb.AppendLine();
            sb.AppendLine($"**Type:** `{property.Type}`");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "generate_docs({\"filePath\": \"Calculator.cs\"})",
            "generate_docs({\"filePath\": \"Service.cs\", \"memberName\": \"ProcessData\", \"style\": \"XML\"})"
        ];
    }
}
