using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Rename;
using System.Text;

namespace Foundz.Net.Tools.Refactoring;

/// <summary>
/// Tool for renaming symbols across the codebase.
/// </summary>
public class RenameSymbolTool : ITool
{
    public string Name => "rename_symbol";
    public string Description => "Rename a symbol (class, method, variable) across the entire codebase with reference updates.";
    public ToolCategory Category => ToolCategory.Refactoring;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Warning;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "projectPath": {
                    "type": "string",
                    "description": "Path to the project or solution file"
                },
                "oldName": {
                    "type": "string",
                    "description": "Current name of the symbol"
                },
                "newName": {
                    "type": "string",
                    "description": "New name for the symbol"
                },
                "dryRun": {
                    "type": "boolean",
                    "description": "Preview changes without applying them",
                    "default": true
                }
            },
            "required": ["projectPath", "oldName", "newName"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("projectPath") || args["projectPath"] is not string path || string.IsNullOrWhiteSpace(path))
            return Task.FromResult(false);

        if (!args.ContainsKey("oldName") || args["oldName"] is not string oldName || string.IsNullOrWhiteSpace(oldName))
            return Task.FromResult(false);

        if (!args.ContainsKey("newName") || args["newName"] is not string newName || string.IsNullOrWhiteSpace(newName))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var projectPath = args["projectPath"].ToString()!;
            var oldName = args["oldName"].ToString()!;
            var newName = args["newName"].ToString()!;
            var dryRun = !args.ContainsKey("dryRun") || Convert.ToBoolean(args["dryRun"]);

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
            
            Solution solution;
            if (projectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                solution = await workspace.OpenSolutionAsync(projectPath, cancellationToken: cancellationToken);
            }
            else
            {
                var proj = await workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
                solution = proj.Solution;
            }

            var project = solution.Projects.FirstOrDefault();
            if (project == null)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = "No project found in solution",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
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

            // Find the symbol to rename
            var symbols = compilation.GetSymbolsWithName(name => name == oldName, SymbolFilter.All, cancellationToken);
            var symbol = symbols.FirstOrDefault();

            if (symbol == null)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Symbol not found: {oldName}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            // Perform the rename
            var newSolution = await Renamer.RenameSymbolAsync(solution, symbol, default, newName, cancellationToken);

            var output = new StringBuilder();
            output.AppendLine($"Rename Symbol: {oldName} → {newName}");
            output.AppendLine($"Symbol Type: {symbol.Kind}");
            output.AppendLine();

            // Get changes
            var changes = newSolution.GetChanges(solution);
            var changedDocuments = 0;
            var totalChanges = 0;

            foreach (var projectChanges in changes.GetProjectChanges())
            {
                foreach (var documentId in projectChanges.GetChangedDocuments())
                {
                    changedDocuments++;
                    var oldDocument = solution.GetDocument(documentId);
                    var newDocument = newSolution.GetDocument(documentId);

                    if (oldDocument != null && newDocument != null)
                    {
                        var oldText = await oldDocument.GetTextAsync(cancellationToken);
                        var newText = await newDocument.GetTextAsync(cancellationToken);
                        
                        var textChanges = newText.GetTextChanges(oldText);
                        totalChanges += textChanges.Count();

                        output.AppendLine($"📄 {oldDocument.Name}:");
                        foreach (var change in textChanges.Take(5))
                        {
                            var lineSpan = oldText.Lines.GetLinePositionSpan(change.Span);
                            output.AppendLine($"  Line {lineSpan.Start.Line + 1}: {change.NewText}");
                        }
                        
                        if (textChanges.Count() > 5)
                        {
                            output.AppendLine($"  ... and {textChanges.Count() - 5} more changes");
                        }
                        
                        output.AppendLine();
                    }
                }
            }

            output.AppendLine($"Summary:");
            output.AppendLine($"  Files affected: {changedDocuments}");
            output.AppendLine($"  Total changes: {totalChanges}");

            if (dryRun)
            {
                output.AppendLine();
                output.AppendLine("ℹ️  This is a DRY RUN. No files were modified.");
                output.AppendLine("   Set dryRun=false to apply these changes.");
            }
            else
            {
                // Apply the changes
                if (workspace.TryApplyChanges(newSolution))
                {
                    output.AppendLine();
                    output.AppendLine("✅ Changes applied successfully!");
                }
                else
                {
                    return new ToolResult
                    {
                        ToolCallId = string.Empty,
                        ToolName = Name,
                        Success = false,
                        Error = "Failed to apply changes to workspace",
                        ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                    };
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
                    ["oldName"] = oldName,
                    ["newName"] = newName,
                    ["filesAffected"] = changedDocuments,
                    ["totalChanges"] = totalChanges,
                    ["dryRun"] = dryRun
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
            "rename_symbol({\"projectPath\": \"Project.csproj\", \"oldName\": \"OldClassName\", \"newName\": \"NewClassName\", \"dryRun\": true})",
            "rename_symbol({\"projectPath\": \"Solution.sln\", \"oldName\": \"Calculate\", \"newName\": \"ComputeResult\", \"dryRun\": false})"
        ];
    }
}
