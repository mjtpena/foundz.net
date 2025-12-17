using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Text;

namespace Foundz.Net.Tools.CodeAnalysis;

/// <summary>
/// Tool for parsing C# code and extracting structure information using Roslyn.
/// </summary>
public class ParseCodeTool : ITool
{
    public string Name => "parse_code";
    public string Description => "Parse C# code and extract classes, methods, properties, and their documentation";
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
                "include_private": {
                    "type": "boolean",
                    "description": "Include private members (default: false)"
                },
                "include_docs": {
                    "type": "boolean",
                    "description": "Include XML documentation (default: true)"
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
            var includePrivate = args.ContainsKey("include_private") && Convert.ToBoolean(args["include_private"]);
            var includeDocs = !args.ContainsKey("include_docs") || Convert.ToBoolean(args["include_docs"]);

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

            // Read and parse the code
            var code = await System.IO.File.ReadAllTextAsync(filePath, cancellationToken);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync(cancellationToken);

            var output = new StringBuilder();
            output.AppendLine($"File: {Path.GetFileName(filePath)}");
            output.AppendLine($"Lines: {code.Split('\n').Length}");
            output.AppendLine();

            // Extract namespaces
            var namespaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();
            var fileScoped = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>();
            
            if (namespaces.Any() || fileScoped.Any())
            {
                output.AppendLine("NAMESPACES:");
                foreach (var ns in namespaces)
                {
                    output.AppendLine($"  - {ns.Name}");
                }
                foreach (var ns in fileScoped)
                {
                    output.AppendLine($"  - {ns.Name}");
                }
                output.AppendLine();
            }

            // Extract using directives
            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
            if (usings.Any())
            {
                output.AppendLine($"USING DIRECTIVES: ({usings.Count()})");
                foreach (var u in usings.Take(10))
                {
                    output.AppendLine($"  - {u.Name}");
                }
                if (usings.Count() > 10)
                    output.AppendLine($"  ... and {usings.Count() - 10} more");
                output.AppendLine();
            }

            // Extract classes
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            if (classes.Any())
            {
                output.AppendLine("CLASSES:");
                foreach (var cls in classes)
                {
                    var modifiers = string.Join(" ", cls.Modifiers.Select(m => m.Text));
                    output.AppendLine($"\n  class {cls.Identifier} ({modifiers})");
                    
                    if (includeDocs)
                    {
                        var doc = GetDocumentation(cls);
                        if (!string.IsNullOrEmpty(doc))
                            output.AppendLine($"    /// {doc}");
                    }

                    // Base types
                    if (cls.BaseList != null)
                    {
                        output.AppendLine($"    Inherits/Implements: {string.Join(", ", cls.BaseList.Types.Select(t => t.Type.ToString()))}");
                    }

                    // Properties
                    var properties = cls.Members.OfType<PropertyDeclarationSyntax>();
                    if (properties.Any())
                    {
                        output.AppendLine($"    Properties ({properties.Count()}):");
                        foreach (var prop in properties)
                        {
                            if (!includePrivate && prop.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
                                continue;

                            var propMods = string.Join(" ", prop.Modifiers.Select(m => m.Text));
                            output.AppendLine($"      - {propMods} {prop.Type} {prop.Identifier}");
                        }
                    }

                    // Methods
                    var methods = cls.Members.OfType<MethodDeclarationSyntax>();
                    if (methods.Any())
                    {
                        output.AppendLine($"    Methods ({methods.Count()}):");
                        foreach (var method in methods)
                        {
                            if (!includePrivate && method.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
                                continue;

                            var methodMods = string.Join(" ", method.Modifiers.Select(m => m.Text));
                            var parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
                            output.AppendLine($"      - {methodMods} {method.ReturnType} {method.Identifier}({parameters})");
                            
                            if (includeDocs)
                            {
                                var methodDoc = GetDocumentation(method);
                                if (!string.IsNullOrEmpty(methodDoc))
                                    output.AppendLine($"        /// {methodDoc}");
                            }
                        }
                    }

                    // Fields
                    var fields = cls.Members.OfType<FieldDeclarationSyntax>();
                    if (fields.Any())
                    {
                        output.AppendLine($"    Fields ({fields.Count()}):");
                        foreach (var field in fields)
                        {
                            if (!includePrivate && field.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
                                continue;

                            var fieldMods = string.Join(" ", field.Modifiers.Select(m => m.Text));
                            foreach (var variable in field.Declaration.Variables)
                            {
                                output.AppendLine($"      - {fieldMods} {field.Declaration.Type} {variable.Identifier}");
                            }
                        }
                    }
                }
                output.AppendLine();
            }

            // Extract interfaces
            var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            if (interfaces.Any())
            {
                output.AppendLine("INTERFACES:");
                foreach (var iface in interfaces)
                {
                    output.AppendLine($"  - {iface.Identifier}");
                    var ifaceMethods = iface.Members.OfType<MethodDeclarationSyntax>();
                    foreach (var method in ifaceMethods)
                    {
                        output.AppendLine($"    - {method.ReturnType} {method.Identifier}(...)");
                    }
                }
                output.AppendLine();
            }

            // Extract enums
            var enums = root.DescendantNodes().OfType<EnumDeclarationSyntax>();
            if (enums.Any())
            {
                output.AppendLine("ENUMS:");
                foreach (var enumDecl in enums)
                {
                    output.AppendLine($"  - {enumDecl.Identifier}");
                    foreach (var member in enumDecl.Members)
                    {
                        output.AppendLine($"    - {member.Identifier}");
                    }
                }
                output.AppendLine();
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
                Error = $"Error parsing code: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    private string GetDocumentation(SyntaxNode node)
    {
        var trivia = node.GetLeadingTrivia()
            .Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || 
                        t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            .FirstOrDefault();

        if (trivia.IsKind(SyntaxKind.None))
            return string.Empty;

        var xml = trivia.GetStructure();
        if (xml is DocumentationCommentTriviaSyntax docComment)
        {
            var summary = docComment.ChildNodes()
                .OfType<XmlElementSyntax>()
                .FirstOrDefault(e => e.StartTag.Name.ToString() == "summary");

            if (summary != null)
            {
                return summary.Content.ToString().Trim();
            }
        }

        return string.Empty;
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """parse_code({"file_path": "Program.cs"})""",
            """parse_code({"file_path": "MyClass.cs", "include_private": true})"""
        };
    }
}
