using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Foundz.Net.Tools.Search;

/// <summary>
/// Tool for searching and analyzing project dependencies.
/// </summary>
public class SearchDependenciesTool : ITool
{
    public string Name => "search_dependencies";
    public string Description => "Search and analyze project dependencies, packages, and their versions.";
    public ToolCategory Category => ToolCategory.Search;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "packageName": {
                    "type": "string",
                    "description": "Name of the package to search for (optional, searches all if not provided)"
                },
                "projectPath": {
                    "type": "string",
                    "description": "Path to project/solution directory (defaults to current directory)"
                },
                "includeTransitive": {
                    "type": "boolean",
                    "description": "Include transitive dependencies",
                    "default": false
                }
            }
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var packageName = args.ContainsKey("packageName") ? args["packageName"].ToString() : null;
            var projectPath = args.ContainsKey("projectPath") ? args["projectPath"].ToString() : Directory.GetCurrentDirectory();
            var includeTransitive = args.ContainsKey("includeTransitive") && Convert.ToBoolean(args["includeTransitive"]);

            if (!Directory.Exists(projectPath))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Directory not found: {projectPath}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var dependencies = new List<DependencyInfo>();

            // Search for .csproj files
            var csprojFiles = Directory.GetFiles(projectPath!, "*.csproj", SearchOption.AllDirectories);
            foreach (var csproj in csprojFiles)
            {
                var deps = await ParseCsProjectDependencies(csproj, cancellationToken);
                dependencies.AddRange(deps);
            }

            // Search for package.json files (for Node.js projects)
            var packageJsonFiles = Directory.GetFiles(projectPath!, "package.json", SearchOption.AllDirectories);
            foreach (var packageJson in packageJsonFiles)
            {
                var deps = await ParsePackageJsonDependencies(packageJson, cancellationToken);
                dependencies.AddRange(deps);
            }

            // Filter by package name if specified
            if (!string.IsNullOrEmpty(packageName))
            {
                dependencies = dependencies
                    .Where(d => d.Name.Contains(packageName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var output = new StringBuilder();
            output.AppendLine("Dependency Analysis:");
            output.AppendLine($"Project Path: {projectPath}");
            
            if (!string.IsNullOrEmpty(packageName))
            {
                output.AppendLine($"Filter: {packageName}");
            }
            
            output.AppendLine($"Total Dependencies Found: {dependencies.Count}");
            output.AppendLine();

            if (dependencies.Any())
            {
                // Group by project file
                var grouped = dependencies.GroupBy(d => d.ProjectFile);
                
                foreach (var group in grouped)
                {
                    output.AppendLine($"📦 {Path.GetFileName(group.Key)}");
                    
                    foreach (var dep in group.OrderBy(d => d.Name))
                    {
                        output.AppendLine($"  ├─ {dep.Name} @ {dep.Version}");
                        
                        if (!string.IsNullOrEmpty(dep.License))
                        {
                            output.AppendLine($"  │  License: {dep.License}");
                        }
                        
                        if (dep.IsTransitive)
                        {
                            output.AppendLine($"  │  (transitive)");
                        }
                    }
                    
                    output.AppendLine();
                }

                // Summary by license
                var licenseGroups = dependencies
                    .Where(d => !string.IsNullOrEmpty(d.License))
                    .GroupBy(d => d.License)
                    .OrderByDescending(g => g.Count());

                if (licenseGroups.Any())
                {
                    output.AppendLine("License Summary:");
                    foreach (var licenseGroup in licenseGroups)
                    {
                        output.AppendLine($"  {licenseGroup.Key}: {licenseGroup.Count()} packages");
                    }
                }
            }
            else
            {
                output.AppendLine("No dependencies found.");
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
                    ["totalDependencies"] = dependencies.Count,
                    ["projectsAnalyzed"] = csprojFiles.Length + packageJsonFiles.Length
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

    private static async Task<List<DependencyInfo>> ParseCsProjectDependencies(string csprojPath, CancellationToken cancellationToken)
    {
        var dependencies = new List<DependencyInfo>();

        try
        {
            var content = await System.IO.File.ReadAllTextAsync(csprojPath, cancellationToken);
            var doc = XDocument.Parse(content);

            var packageReferences = doc.Descendants("PackageReference");
            foreach (var packageRef in packageReferences)
            {
                var name = packageRef.Attribute("Include")?.Value;
                var version = packageRef.Attribute("Version")?.Value;

                if (!string.IsNullOrEmpty(name))
                {
                    dependencies.Add(new DependencyInfo
                    {
                        Name = name,
                        Version = version ?? "unknown",
                        ProjectFile = csprojPath,
                        Type = "NuGet",
                        IsTransitive = false
                    });
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return dependencies;
    }

    private static async Task<List<DependencyInfo>> ParsePackageJsonDependencies(string packageJsonPath, CancellationToken cancellationToken)
    {
        var dependencies = new List<DependencyInfo>();

        try
        {
            var content = await System.IO.File.ReadAllTextAsync(packageJsonPath, cancellationToken);
            var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("dependencies", out var deps))
            {
                foreach (var dep in deps.EnumerateObject())
                {
                    dependencies.Add(new DependencyInfo
                    {
                        Name = dep.Name,
                        Version = dep.Value.GetString() ?? "unknown",
                        ProjectFile = packageJsonPath,
                        Type = "npm",
                        IsTransitive = false
                    });
                }
            }

            if (doc.RootElement.TryGetProperty("devDependencies", out var devDeps))
            {
                foreach (var dep in devDeps.EnumerateObject())
                {
                    dependencies.Add(new DependencyInfo
                    {
                        Name = dep.Name,
                        Version = dep.Value.GetString() ?? "unknown",
                        ProjectFile = packageJsonPath,
                        Type = "npm (dev)",
                        IsTransitive = false
                    });
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return dependencies;
    }

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "search_dependencies({})",
            "search_dependencies({\"packageName\": \"Microsoft.Extensions\"})",
            "search_dependencies({\"projectPath\": \"./src\", \"includeTransitive\": true})"
        ];
    }

    private record DependencyInfo
    {
        public required string Name { get; init; }
        public required string Version { get; init; }
        public required string ProjectFile { get; init; }
        public required string Type { get; init; }
        public required bool IsTransitive { get; init; }
        public string? License { get; init; }
    }
}
