using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Foundz.Net.Tools.Refactoring;

/// <summary>
/// Tool for moving a class to a different file and/or namespace.
/// Handles updating imports and references automatically.
/// </summary>
public class MoveClassTool : ITool
{
    public string Name => "move_class";
    public string Description => "Move a class to a different file and/or namespace. Automatically updates usings and references.";
    public ToolCategory Category => ToolCategory.Refactoring;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Warning;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "sourceFilePath": {
                    "type": "string",
                    "description": "Path to the current file containing the class"
                },
                "className": {
                    "type": "string",
                    "description": "Name of the class to move"
                },
                "targetFilePath": {
                    "type": "string",
                    "description": "Path to the target file (will be created if doesn't exist)"
                },
                "targetNamespace": {
                    "type": "string",
                    "description": "Optional: New namespace for the class. If not specified, keeps current namespace."
                },
                "deleteEmptySource": {
                    "type": "boolean",
                    "description": "If true, delete source file if it becomes empty after moving the class",
                    "default": true
                },
                "dryRun": {
                    "type": "boolean",
                    "description": "If true, show preview without applying changes",
                    "default": false
                }
            },
            "required": ["sourceFilePath", "className", "targetFilePath"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("sourceFilePath") || args["sourceFilePath"] is not string sourcePath || string.IsNullOrWhiteSpace(sourcePath))
            return Task.FromResult(false);

        if (!args.ContainsKey("className") || args["className"] is not string className || string.IsNullOrWhiteSpace(className))
            return Task.FromResult(false);

        if (!args.ContainsKey("targetFilePath") || args["targetFilePath"] is not string targetPath || string.IsNullOrWhiteSpace(targetPath))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceFilePath = (string)args["sourceFilePath"];
            var className = (string)args["className"];
            var targetFilePath = (string)args["targetFilePath"];
            var targetNamespace = args.GetValueOrDefault("targetNamespace") as string;
            var deleteEmptySource = args.GetValueOrDefault("deleteEmptySource") as bool? ?? true;
            var dryRun = args.GetValueOrDefault("dryRun") as bool? ?? false;

            if (!System.IO.File.Exists(sourceFilePath))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Source file not found: {sourceFilePath}"
                };
            }

            // Parse source file
            var sourceCode = await System.IO.File.ReadAllTextAsync(sourceFilePath, cancellationToken);
            var sourceTree = CSharpSyntaxTree.ParseText(sourceCode, cancellationToken: cancellationToken);
            var sourceRoot = await sourceTree.GetRootAsync(cancellationToken);

            // Find the class to move
            var classDeclaration = sourceRoot.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == className);

            if (classDeclaration == null)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Class '{className}' not found in source file"
                };
            }

            // Get current namespace
            var currentNamespace = GetNamespace(classDeclaration);
            var newNamespace = !string.IsNullOrWhiteSpace(targetNamespace) ? targetNamespace : currentNamespace;

            // Get usings from source file
            var usings = sourceRoot.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .ToList();

            // Build target file content
            string targetContent;
            CompilationUnitSyntax targetRoot;

            if (System.IO.File.Exists(targetFilePath))
            {
                // Target file exists - append to it
                var existingCode = await System.IO.File.ReadAllTextAsync(targetFilePath, cancellationToken);
                var existingTree = CSharpSyntaxTree.ParseText(existingCode, cancellationToken: cancellationToken);
                targetRoot = (CompilationUnitSyntax)await existingTree.GetRootAsync(cancellationToken);

                // Add any missing usings
                var existingUsings = targetRoot.Usings.Select(u => u.Name?.ToString()).ToHashSet();
                foreach (var usingDirective in usings)
                {
                    var usingName = usingDirective.Name?.ToString();
                    if (usingName != null && !existingUsings.Contains(usingName))
                    {
                        targetRoot = targetRoot.AddUsings(usingDirective);
                    }
                }

                // Find namespace or file-scoped namespace in target
                var targetNs = targetRoot.DescendantNodes()
                    .OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault(ns => ns.Name.ToString() == newNamespace);

                var targetFileScopedNs = targetRoot.DescendantNodes()
                    .OfType<FileScopedNamespaceDeclarationSyntax>()
                    .FirstOrDefault(ns => ns.Name.ToString() == newNamespace);

                if (targetFileScopedNs != null)
                {
                    // Add to file-scoped namespace
                    targetRoot = targetRoot.ReplaceNode(
                        targetFileScopedNs,
                        targetFileScopedNs.AddMembers(classDeclaration));
                }
                else if (targetNs != null)
                {
                    // Add to existing namespace
                    targetRoot = targetRoot.ReplaceNode(
                        targetNs,
                        targetNs.AddMembers(classDeclaration));
                }
                else
                {
                    // Create new namespace
                    var newNs = SyntaxFactory.FileScopedNamespaceDeclaration(
                        SyntaxFactory.ParseName(newNamespace))
                        .AddMembers(classDeclaration);
                    targetRoot = targetRoot.AddMembers(newNs);
                }

                targetContent = targetRoot.NormalizeWhitespace().ToFullString();
            }
            else
            {
                // Create new target file
                var usingsList = usings.Select(u => u.NormalizeWhitespace()).ToList();
                var compilationUnit = SyntaxFactory.CompilationUnit()
                    .AddUsings(usingsList.ToArray());

                var fileScopedNs = SyntaxFactory.FileScopedNamespaceDeclaration(
                    SyntaxFactory.ParseName(newNamespace))
                    .AddMembers(classDeclaration);

                compilationUnit = compilationUnit.AddMembers(fileScopedNs);
                targetContent = compilationUnit.NormalizeWhitespace().ToFullString();
            }

            // Remove class from source file
            var modifiedSourceRoot = sourceRoot.RemoveNode(
                classDeclaration,
                SyntaxRemoveOptions.KeepNoTrivia);

            if (modifiedSourceRoot == null)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = "Failed to remove class from source file"
                };
            }

            // Check if source file is now empty (only usings and namespace)
            var remainingClasses = modifiedSourceRoot.DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .Any();

            string? modifiedSourceContent = null;
            bool willDeleteSource = false;

            if (!remainingClasses && deleteEmptySource)
            {
                willDeleteSource = true;
            }
            else
            {
                modifiedSourceContent = modifiedSourceRoot.NormalizeWhitespace().ToFullString();
            }

            if (dryRun)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = true,
                    Output = $"Dry run: Would move class '{className}' from '{sourceFilePath}' to '{targetFilePath}'",
                    Metadata = new Dictionary<string, object>
                    {
                        ["sourceFile"] = sourceFilePath,
                        ["targetFile"] = targetFilePath,
                        ["targetContent"] = targetContent,
                        ["modifiedSource"] = modifiedSourceContent ?? "[DELETE]",
                        ["willDeleteSource"] = willDeleteSource,
                        ["namespaceChanged"] = currentNamespace != newNamespace,
                        ["oldNamespace"] = currentNamespace ?? "",
                        ["newNamespace"] = newNamespace,
                        ["dryRun"] = true
                    }
                };
            }

            // Ensure target directory exists
            var targetDirectory = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // Write target file
            await System.IO.File.WriteAllTextAsync(targetFilePath, targetContent, cancellationToken);

            // Handle source file
            if (willDeleteSource)
            {
                System.IO.File.Delete(sourceFilePath);
            }
            else
            {
                await System.IO.File.WriteAllTextAsync(sourceFilePath, modifiedSourceContent!, cancellationToken);
            }

            var message = willDeleteSource
                ? $"Successfully moved class '{className}' to '{targetFilePath}' and deleted empty source file"
                : $"Successfully moved class '{className}' to '{targetFilePath}'";

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = message,
                Metadata = new Dictionary<string, object>
                {
                    ["sourceFile"] = sourceFilePath,
                    ["targetFile"] = targetFilePath,
                    ["sourceDeleted"] = willDeleteSource,
                    ["namespaceChanged"] = currentNamespace != newNamespace,
                    ["oldNamespace"] = currentNamespace ?? "",
                    ["newNamespace"] = newNamespace
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
                Error = $"Failed to move class: {ex.Message}"
            };
        }
    }

    private string? GetNamespace(SyntaxNode node)
    {
        var namespaceDeclaration = node.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
        if (namespaceDeclaration != null)
            return namespaceDeclaration.Name.ToString();

        var fileScopedNamespace = node.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>();
        if (fileScopedNamespace != null)
            return fileScopedNamespace.Name.ToString();

        return null;
    }

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "move_class({\"sourceFilePath\": \"src/Services/OldFile.cs\", \"className\": \"UserService\", \"targetFilePath\": \"src/Services/UserService.cs\", \"dryRun\": true})",
            "move_class({\"sourceFilePath\": \"src/Data/Repository.cs\", \"className\": \"UserRepository\", \"targetFilePath\": \"src/Services/Data/UserRepository.cs\", \"targetNamespace\": \"MyApp.Services.Data\"})"
        ];
    }
}
