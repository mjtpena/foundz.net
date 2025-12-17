using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Diagnostics;
using System.Text;

namespace Foundz.Net.Tools.Shell;

/// <summary>
/// Tool for building projects in various languages.
/// </summary>
public class BuildProjectTool : ITool
{
    public string Name => "build_project";
    public string Description => "Build a project using detected build system (dotnet, npm, cargo, maven, gradle)";
    public ToolCategory Category => ToolCategory.ShellExecution;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to project directory (default: current directory)"
                },
                "configuration": {
                    "type": "string",
                    "description": "Build configuration: Debug or Release (default: Debug)"
                },
                "clean": {
                    "type": "boolean",
                    "description": "Clean before building (default: false)"
                },
                "build_system": {
                    "type": "string",
                    "description": "Build system: dotnet, npm, cargo, maven, gradle (auto-detected if not specified)"
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
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var path = args.ContainsKey("path") ? args["path"].ToString()! : Directory.GetCurrentDirectory();
            var configuration = args.ContainsKey("configuration") ? args["configuration"].ToString()! : "Debug";
            var clean = args.ContainsKey("clean") && Convert.ToBoolean(args["clean"]);
            var buildSystem = args.ContainsKey("build_system") ? args["build_system"].ToString() : null;

            if (!Directory.Exists(path))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Directory not found: {path}",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            // Auto-detect build system if not specified
            if (string.IsNullOrEmpty(buildSystem))
            {
                buildSystem = DetectBuildSystem(path);
            }

            if (string.IsNullOrEmpty(buildSystem))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = "Could not detect build system. Specify 'build_system' parameter.",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            var output = new StringBuilder();

            // Clean if requested
            if (clean)
            {
                var (cleanCmd, cleanArgs) = BuildCleanCommand(buildSystem, path, configuration);
                var cleanResult = await ExecuteCommand(cleanCmd, cleanArgs, path, cancellationToken);
                output.AppendLine("=== CLEAN ===");
                output.AppendLine(cleanResult);
                output.AppendLine();
            }

            // Build
            var (buildCmd, buildArgs) = BuildBuildCommand(buildSystem, path, configuration);
            var buildResult = await ExecuteCommand(buildCmd, buildArgs, path, cancellationToken);
            
            output.AppendLine("=== BUILD ===");
            output.AppendLine(buildResult);

            stopwatch.Stop();

            // Check for success indicators
            var outputText = output.ToString();
            var success = IsSuccessful(outputText, buildSystem);

            if (success)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = true,
                    Output = $"✓ Build succeeded!\n\n{outputText}",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
            else
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"✗ Build failed!\n\n{outputText}",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = false,
                Error = $"Error building project: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    private string? DetectBuildSystem(string path)
    {
        // Check for .NET projects
        if (Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Any() ||
            Directory.GetFiles(path, "*.sln", SearchOption.TopDirectoryOnly).Any())
            return "dotnet";

        // Check for Node.js projects
        if (System.IO.File.Exists(Path.Combine(path, "package.json")))
            return "npm";

        // Check for Rust projects
        if (System.IO.File.Exists(Path.Combine(path, "Cargo.toml")))
            return "cargo";

        // Check for Maven projects
        if (System.IO.File.Exists(Path.Combine(path, "pom.xml")))
            return "maven";

        // Check for Gradle projects
        if (System.IO.File.Exists(Path.Combine(path, "build.gradle")) || 
            System.IO.File.Exists(Path.Combine(path, "build.gradle.kts")))
            return "gradle";

        return null;
    }

    private (string command, string arguments) BuildBuildCommand(string buildSystem, string path, string configuration)
    {
        return buildSystem.ToLower() switch
        {
            "dotnet" => ("dotnet", $"build -c {configuration}"),
            "npm" => ("npm", "run build"),
            "cargo" => ("cargo", configuration == "Release" ? "build --release" : "build"),
            "maven" => ("mvn", "compile"),
            "gradle" => ("gradle", "build"),
            _ => ("dotnet", "build")
        };
    }

    private (string command, string arguments) BuildCleanCommand(string buildSystem, string path, string configuration)
    {
        return buildSystem.ToLower() switch
        {
            "dotnet" => ("dotnet", "clean"),
            "npm" => ("npm", "run clean"),
            "cargo" => ("cargo", "clean"),
            "maven" => ("mvn", "clean"),
            "gradle" => ("gradle", "clean"),
            _ => ("dotnet", "clean")
        };
    }

    private async Task<string> ExecuteCommand(string command, string arguments, string workingDir, CancellationToken cancellationToken)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var output = new StringBuilder();

        using var process = new Process { StartInfo = processStartInfo };
        
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                output.AppendLine(e.Data);
        };
        
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                output.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return output.ToString();
    }

    private bool IsSuccessful(string output, string buildSystem)
    {
        var lowerOutput = output.ToLower();
        return buildSystem.ToLower() switch
        {
            "dotnet" => lowerOutput.Contains("build succeeded") && !lowerOutput.Contains("build failed"),
            "npm" => !lowerOutput.Contains("error") && !lowerOutput.Contains("failed"),
            "cargo" => lowerOutput.Contains("finished") && !lowerOutput.Contains("error"),
            "maven" => lowerOutput.Contains("build success"),
            "gradle" => lowerOutput.Contains("build successful"),
            _ => !lowerOutput.Contains("error") && !lowerOutput.Contains("failed")
        };
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """build_project({})""",
            """build_project({"path": "/path/to/project", "configuration": "Release"})""",
            """build_project({"build_system": "dotnet", "clean": true})"""
        };
    }
}
