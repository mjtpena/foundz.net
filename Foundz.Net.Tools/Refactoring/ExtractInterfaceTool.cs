using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Foundz.Net.Tools.Refactoring;

/// <summary>
/// Tool for extracting an interface from a class with selected public members.
/// Uses Roslyn for semantic analysis and code generation.
/// </summary>
public class ExtractInterfaceTool : ITool
{
    public string Name => "extract_interface";
    public string Description => "Extract an interface from a class, including selected public members. Generates interface file and updates class to implement it.";
    public ToolCategory Category => ToolCategory.Refactoring;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Warning;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "filePath": {
                    "type": "string",
                    "description": "Path to the C# source file containing the class"
                },
                "className": {
                    "type": "string",
                    "description": "Name of the class to extract interface from"
                },
                "interfaceName": {
                    "type": "string",
                    "description": "Name for the new interface (e.g., 'IMyService')"
                },
                "members": {
                    "type": "array",
                    "description": "Optional: Specific member names to include. If not provided, includes all public members.",
                    "items": {
                        "type": "string"
                    }
                },
                "outputPath": {
                    "type": "string",
                    "description": "Optional: Path for the new interface file. Defaults to same directory."
                },
                "dryRun": {
                    "type": "boolean",
                    "description": "If true, show preview without creating files",
                    "default": false
                }
            },
            "required": ["filePath", "className", "interfaceName"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("filePath") || args["filePath"] is not string path || string.IsNullOrWhiteSpace(path))
            return Task.FromResult(false);

        if (!args.ContainsKey("className") || args["className"] is not string className || string.IsNullOrWhiteSpace(className))
            return Task.FromResult(false);

        if (!args.ContainsKey("interfaceName") || args["interfaceName"] is not string interfaceName || string.IsNullOrWhiteSpace(interfaceName))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = (string)args["filePath"];
            var className = (string)args["className"];
            var interfaceName = (string)args["interfaceName"];
            var dryRun = args.GetValueOrDefault("dryRun") as bool? ?? false;
            var memberFilter = args.GetValueOrDefault("members") as List<string>;

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

            // Find the class
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
                    Error = $"Class '{className}' not found in file"
                };
            }

            // Extract public members
            var publicMethods = classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)))
                .Where(m => memberFilter == null || memberFilter.Contains(m.Identifier.Text))
                .ToList();

            var publicProperties = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(p => p.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)))
                .Where(p => memberFilter == null || memberFilter.Contains(p.Identifier.Text))
                .ToList();

            if (!publicMethods.Any() && !publicProperties.Any())
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"No public members found in class '{className}'"
                };
            }

            // Build interface code
            var interfaceCode = BuildInterface(classDeclaration, interfaceName, publicMethods, publicProperties);

            // Determine output path
            var outputPath = args.GetValueOrDefault("outputPath") as string;
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                var directory = Path.GetDirectoryName(filePath) ?? "";
                outputPath = Path.Combine(directory, $"{interfaceName}.cs");
            }

            // Update class to implement interface
            var baseList = classDeclaration.BaseList;
            BaseListSyntax newBaseList;
            
            if (baseList == null)
            {
                newBaseList = SyntaxFactory.BaseList(
                    SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(interfaceName))));
            }
            else
            {
                newBaseList = baseList.AddTypes(
                    SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(interfaceName)));
            }

            var modifiedClass = classDeclaration.WithBaseList(newBaseList);
            var modifiedRoot = root.ReplaceNode(classDeclaration, modifiedClass);
            var modifiedCode = modifiedRoot.ToFullString();

            if (dryRun)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = true,
                    Output = $"Dry run: Would create interface '{interfaceName}' with {publicMethods.Count} method(s) and {publicProperties.Count} property/properties",
                    Metadata = new Dictionary<string, object>
                    {
                        ["interfaceCode"] = interfaceCode,
                        ["outputPath"] = outputPath,
                        ["methodCount"] = publicMethods.Count,
                        ["propertyCount"] = publicProperties.Count,
                        ["modifiedClass"] = modifiedCode,
                        ["dryRun"] = true
                    }
                };
            }

            // Create interface file
            await System.IO.File.WriteAllTextAsync(outputPath, interfaceCode, cancellationToken);

            // Update original class
            await System.IO.File.WriteAllTextAsync(filePath, modifiedCode, cancellationToken);

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = $"Successfully extracted interface '{interfaceName}' with {publicMethods.Count} method(s) and {publicProperties.Count} property/properties",
                Metadata = new Dictionary<string, object>
                {
                    ["interfacePath"] = outputPath,
                    ["classPath"] = filePath,
                    ["methodCount"] = publicMethods.Count,
                    ["propertyCount"] = publicProperties.Count
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
                Error = $"Failed to extract interface: {ex.Message}"
            };
        }
    }

    private string BuildInterface(
        ClassDeclarationSyntax classDeclaration,
        string interfaceName,
        List<MethodDeclarationSyntax> methods,
        List<PropertyDeclarationSyntax> properties)
    {
        var namespaceDeclaration = classDeclaration.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
        var fileScopedNamespace = classDeclaration.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>();
        var namespaceName = namespaceDeclaration?.Name.ToString() ?? fileScopedNamespace?.Name.ToString() ?? "GeneratedInterfaces";

        var interfaceMembers = new List<string>();

        // Add properties
        foreach (var prop in properties)
        {
            var accessors = new List<string>();
            if (prop.AccessorList != null)
            {
                foreach (var accessor in prop.AccessorList.Accessors)
                {
                    if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                        accessors.Add("get;");
                    if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration) || accessor.IsKind(SyntaxKind.InitAccessorDeclaration))
                        accessors.Add("set;");
                }
            }
            var accessorString = accessors.Any() ? $" {{ {string.Join(" ", accessors)} }}" : ";";
            interfaceMembers.Add($"    {prop.Type} {prop.Identifier}{accessorString}");
        }

        // Add methods
        foreach (var method in methods)
        {
            var parameters = method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}");
            var signature = $"{method.ReturnType} {method.Identifier}({string.Join(", ", parameters)})";
            interfaceMembers.Add($"    {signature};");
        }

        var interfaceCode = $@"namespace {namespaceName};

/// <summary>
/// Interface extracted from {classDeclaration.Identifier.Text}
/// </summary>
public interface {interfaceName}
{{
{string.Join("\n\n", interfaceMembers)}
}}
";

        return interfaceCode;
    }

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "extract_interface({\"filePath\": \"src/Services/UserService.cs\", \"className\": \"UserService\", \"interfaceName\": \"IUserService\", \"dryRun\": true})",
            "extract_interface({\"filePath\": \"src/Data/Repository.cs\", \"className\": \"Repository\", \"interfaceName\": \"IRepository\"})"
        ];
    }
}
