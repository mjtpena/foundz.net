using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text;

namespace Foundz.Net.Tools.CodeAnalysis;

/// <summary>
/// Tool for finding the definition of a symbol.
/// </summary>
public class GetDefinitionTool : ITool
{
    public string Name => "get_definition";
    public string Description => "Get the definition location and documentation for a symbol.";
    public ToolCategory Category => ToolCategory.CodeAnalysis;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "symbolName": {
                    "type": "string",
                    "description": "Name of the symbol to find definition for"
                },
                "projectPath": {
                    "type": "string",
                    "description": "Path to the project or solution file"
                },
                "includeDocumentation": {
                    "type": "boolean",
                    "description": "Include XML documentation if available",
                    "default": true
                }
            },
            "required": ["symbolName", "projectPath"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("symbolName") || args["symbolName"] is not string symbol || string.IsNullOrWhiteSpace(symbol))
            return Task.FromResult(false);

        if (!args.ContainsKey("projectPath") || args["projectPath"] is not string path || string.IsNullOrWhiteSpace(path))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var symbolName = args["symbolName"].ToString()!;
            var projectPath = args["projectPath"].ToString()!;
            var includeDocumentation = !args.ContainsKey("includeDocumentation") || Convert.ToBoolean(args["includeDocumentation"]);

            if (!System.IO.File.Exists(projectPath))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Project file not found: {projectPath}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            using var workspace = MSBuildWorkspace.Create();
            
            Project project;
            if (projectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                var solution = await workspace.OpenSolutionAsync(projectPath, cancellationToken: cancellationToken);
                project = solution.Projects.FirstOrDefault()!;
            }
            else
            {
                project = await workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
            }

            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation == null)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = "Failed to compile project",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var symbols = compilation.GetSymbolsWithName(name => name.Contains(symbolName), SymbolFilter.All, cancellationToken);
            var symbol = symbols.FirstOrDefault();

            if (symbol == null)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Symbol not found: {symbolName}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var output = new StringBuilder();
            output.AppendLine($"Definition of '{symbolName}':");
            output.AppendLine();
            output.AppendLine($"Full Name: {symbol.ToDisplayString()}");
            output.AppendLine($"Kind: {symbol.Kind}");
            
            var location = symbol.Locations.FirstOrDefault();
            if (location != null && location.IsInSource)
            {
                var lineSpan = location.GetLineSpan();
                output.AppendLine($"File: {lineSpan.Path}");
                output.AppendLine($"Line: {lineSpan.StartLinePosition.Line + 1}");
                output.AppendLine($"Column: {lineSpan.StartLinePosition.Character + 1}");
            }

            if (includeDocumentation)
            {
                var xmlDoc = symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);
                if (!string.IsNullOrWhiteSpace(xmlDoc))
                {
                    output.AppendLine();
                    output.AppendLine("Documentation:");
                    output.AppendLine(xmlDoc);
                }
            }

            // Show signature for methods
            if (symbol is IMethodSymbol method)
            {
                output.AppendLine();
                output.AppendLine("Signature:");
                output.AppendLine($"  {method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                output.AppendLine($"  Returns: {method.ReturnType.ToDisplayString()}");
                
                if (method.Parameters.Any())
                {
                    output.AppendLine("  Parameters:");
                    foreach (var param in method.Parameters)
                    {
                        output.AppendLine($"    - {param.Name}: {param.Type.ToDisplayString()}");
                    }
                }
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
                    ["symbolName"] = symbolName,
                    ["symbolKind"] = symbol.Kind.ToString()
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
            "get_definition({\"symbolName\": \"MyClass\", \"projectPath\": \"Project.csproj\"})",
            "get_definition({\"symbolName\": \"Calculate\", \"projectPath\": \"Solution.sln\"})"
        ];
    }
}
