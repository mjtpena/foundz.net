using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text;

namespace Foundz.Net.Tools.CodeAnalysis;

/// <summary>
/// Tool for running static code analysis and linting.
/// </summary>
public class LintCodeTool : ITool
{
    public string Name => "lint_code";
    public string Description => "Run static analyzers and linters to detect code style violations and potential issues.";
    public ToolCategory Category => ToolCategory.CodeAnalysis;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "projectPath": {
                    "type": "string",
                    "description": "Path to the project or solution file"
                },
                "severity": {
                    "type": "string",
                    "description": "Minimum severity level to report",
                    "enum": ["Info", "Warning", "Error"],
                    "default": "Warning"
                },
                "maxIssues": {
                    "type": "integer",
                    "description": "Maximum number of issues to return",
                    "default": 50
                }
            },
            "required": ["projectPath"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("projectPath") || args["projectPath"] is not string path || string.IsNullOrWhiteSpace(path))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var projectPath = args["projectPath"].ToString()!;
            var severityStr = args.ContainsKey("severity") ? args["severity"].ToString() : "Warning";
            var maxIssues = args.ContainsKey("maxIssues") ? Convert.ToInt32(args["maxIssues"]) : 50;

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

            var minSeverity = Enum.Parse<DiagnosticSeverity>(severityStr!, ignoreCase: true);

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

            var diagnostics = compilation.GetDiagnostics(cancellationToken)
                .Where(d => d.Severity >= minSeverity && !d.IsSuppressed)
                .OrderByDescending(d => d.Severity)
                .ThenBy(d => d.Location.SourceTree?.FilePath)
                .Take(maxIssues)
                .ToList();

            var output = new StringBuilder();
            output.AppendLine($"Lint Results for: {Path.GetFileName(projectPath)}");
            output.AppendLine($"Minimum Severity: {minSeverity}");
            output.AppendLine();

            var errorCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
            var warningCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
            var infoCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Info);

            output.AppendLine($"Summary: {errorCount} errors, {warningCount} warnings, {infoCount} info");
            output.AppendLine();

            if (diagnostics.Any())
            {
                output.AppendLine("Issues:");
                foreach (var diagnostic in diagnostics)
                {
                    var location = diagnostic.Location.GetLineSpan();
                    var severity = diagnostic.Severity switch
                    {
                        DiagnosticSeverity.Error => "❌ ERROR",
                        DiagnosticSeverity.Warning => "⚠️  WARN",
                        DiagnosticSeverity.Info => "ℹ️  INFO",
                        _ => "   "
                    };

                    output.AppendLine($"{severity} {diagnostic.Id}: {diagnostic.GetMessage()}");
                    
                    if (location.IsValid)
                    {
                        output.AppendLine($"       at {Path.GetFileName(location.Path)}:{location.StartLinePosition.Line + 1}:{location.StartLinePosition.Character + 1}");
                    }
                    
                    output.AppendLine();
                }

                if (compilation.GetDiagnostics(cancellationToken).Count(d => d.Severity >= minSeverity) > maxIssues)
                {
                    output.AppendLine($"... and more issues (showing first {maxIssues})");
                }
            }
            else
            {
                output.AppendLine("✅ No issues found!");
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
                    ["errorCount"] = errorCount,
                    ["warningCount"] = warningCount,
                    ["infoCount"] = infoCount,
                    ["totalIssues"] = diagnostics.Count
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
            "lint_code({\"projectPath\": \"Project.csproj\"})",
            "lint_code({\"projectPath\": \"Solution.sln\", \"severity\": \"Error\"})"
        ];
    }
}
