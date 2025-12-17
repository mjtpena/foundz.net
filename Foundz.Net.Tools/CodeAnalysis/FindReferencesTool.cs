using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text;

namespace Foundz.Net.Tools.CodeAnalysis;

/// <summary>
/// Tool for finding all references to a symbol across the codebase.
/// </summary>
public class FindReferencesTool : ITool
{
    public string Name => "find_references";
    public string Description => "Find all references to a symbol (class, method, property, etc.) across the codebase.";
    public ToolCategory Category => ToolCategory.CodeAnalysis;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "symbolName": {
                    "type": "string",
                    "description": "Name of the symbol to find references for"
                },
                "projectPath": {
                    "type": "string",
                    "description": "Path to the project or solution file"
                },
                "maxResults": {
                    "type": "integer",
                    "description": "Maximum number of results to return",
                    "default": 100
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
            var maxResults = args.ContainsKey("maxResults") ? Convert.ToInt32(args["maxResults"]) : 100;

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

            // Find the symbol
            var symbols = compilation.GetSymbolsWithName(name => name.Contains(symbolName), SymbolFilter.All, cancellationToken);
            var targetSymbol = symbols.FirstOrDefault();

            if (targetSymbol == null)
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

            // Find all references
            var references = await SymbolFinder.FindReferencesAsync(targetSymbol, project.Solution, cancellationToken);
            
            var output = new StringBuilder();
            output.AppendLine($"References to '{symbolName}':");
            output.AppendLine($"Symbol: {targetSymbol.ToDisplayString()}");
            output.AppendLine();

            var count = 0;
            foreach (var reference in references)
            {
                foreach (var location in reference.Locations.Take(maxResults - count))
                {
                    var lineSpan = location.Location.GetLineSpan();
                    output.AppendLine($"  {lineSpan.Path}:{lineSpan.StartLinePosition.Line + 1}");
                    
                    count++;
                    if (count >= maxResults)
                        break;
                }
                
                if (count >= maxResults)
                    break;
            }

            if (count == 0)
            {
                output.AppendLine("  (no references found)");
            }
            else if (count >= maxResults)
            {
                output.AppendLine($"\n  ... and more (showing first {maxResults})");
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
                    ["referenceCount"] = count
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
            "find_references({\"symbolName\": \"MyClass\", \"projectPath\": \"Project.csproj\"})",
            "find_references({\"symbolName\": \"Calculate\", \"projectPath\": \"Solution.sln\", \"maxResults\": 50})"
        ];
    }
}
