using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Text;
using System.Xml.Linq;

namespace Foundz.Net.Tools.Testing;

/// <summary>
/// Tool for analyzing code coverage reports.
/// </summary>
public class AnalyzeCoverageTool : ITool
{
    public string Name => "analyze_coverage";
    public string Description => "Analyze code coverage reports and identify uncovered code.";
    public ToolCategory Category => ToolCategory.Testing;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "coverageFile": {
                    "type": "string",
                    "description": "Path to the coverage report file (Cobertura XML format)"
                },
                "threshold": {
                    "type": "number",
                    "description": "Minimum coverage threshold (0-100)",
                    "default": 80
                },
                "showUncovered": {
                    "type": "boolean",
                    "description": "Show details of uncovered code",
                    "default": true
                }
            },
            "required": ["coverageFile"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("coverageFile") || args["coverageFile"] is not string file || string.IsNullOrWhiteSpace(file))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var coverageFile = args["coverageFile"].ToString()!;
            var threshold = args.ContainsKey("threshold") ? Convert.ToDouble(args["threshold"]) : 80.0;
            var showUncovered = !args.ContainsKey("showUncovered") || Convert.ToBoolean(args["showUncovered"]);

            if (!System.IO.File.Exists(coverageFile))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Coverage file not found: {coverageFile}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var content = await System.IO.File.ReadAllTextAsync(coverageFile, cancellationToken);
            var doc = XDocument.Parse(content);

            var output = new StringBuilder();
            output.AppendLine("Code Coverage Analysis");
            output.AppendLine("=====================");
            output.AppendLine();

            // Parse Cobertura format
            var coverage = doc.Root;
            if (coverage?.Name.LocalName == "coverage")
            {
                var lineRate = double.Parse(coverage.Attribute("line-rate")?.Value ?? "0");
                var branchRate = double.Parse(coverage.Attribute("branch-rate")?.Value ?? "0");
                var linesCovered = int.Parse(coverage.Attribute("lines-covered")?.Value ?? "0");
                var linesValid = int.Parse(coverage.Attribute("lines-valid")?.Value ?? "0");

                var overallCoverage = lineRate * 100;

                output.AppendLine($"Overall Line Coverage: {overallCoverage:F2}%");
                output.AppendLine($"Branch Coverage: {branchRate * 100:F2}%");
                output.AppendLine($"Lines Covered: {linesCovered}/{linesValid}");
                output.AppendLine($"Threshold: {threshold}%");
                output.AppendLine();

                if (overallCoverage >= threshold)
                {
                    output.AppendLine($"✅ Coverage meets threshold ({threshold}%)");
                }
                else
                {
                    output.AppendLine($"❌ Coverage below threshold (need {threshold - overallCoverage:F2}% more)");
                }

                output.AppendLine();
                output.AppendLine("Coverage by Package:");
                output.AppendLine();

                var packages = coverage.Descendants("package");
                foreach (var package in packages)
                {
                    var pkgName = package.Attribute("name")?.Value ?? "Unknown";
                    var pkgLineRate = double.Parse(package.Attribute("line-rate")?.Value ?? "0");

                    output.AppendLine($"  📦 {pkgName}: {pkgLineRate * 100:F2}%");

                    if (showUncovered)
                    {
                        var classes = package.Descendants("class");
                        foreach (var cls in classes.Where(c => double.Parse(c.Attribute("line-rate")?.Value ?? "1") < 1.0))
                        {
                            var clsName = cls.Attribute("name")?.Value ?? "Unknown";
                            var clsLineRate = double.Parse(cls.Attribute("line-rate")?.Value ?? "0");

                            if (clsLineRate < threshold / 100.0)
                            {
                                output.AppendLine($"    ⚠️  {Path.GetFileName(clsName)}: {clsLineRate * 100:F2}%");

                                // Show uncovered lines
                                var lines = cls.Descendants("line")
                                    .Where(l => l.Attribute("hits")?.Value == "0")
                                    .Take(5);

                                foreach (var line in lines)
                                {
                                    var lineNum = line.Attribute("number")?.Value;
                                    output.AppendLine($"        Line {lineNum}: not covered");
                                }
                            }
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
                        ["overallCoverage"] = overallCoverage,
                        ["threshold"] = threshold,
                        ["meetsThreshold"] = overallCoverage >= threshold,
                        ["linesCovered"] = linesCovered,
                        ["linesValid"] = linesValid
                    }
                };
            }

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = false,
                Error = "Invalid coverage report format. Expected Cobertura XML.",
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
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
            "analyze_coverage({\"coverageFile\": \"coverage.cobertura.xml\"})",
            "analyze_coverage({\"coverageFile\": \"coverage.xml\", \"threshold\": 90})"
        ];
    }
}
