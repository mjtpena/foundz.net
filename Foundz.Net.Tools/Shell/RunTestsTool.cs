using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Diagnostics;
using System.Text;

namespace Foundz.Net.Tools.Shell;

/// <summary>
/// Tool for running tests in various frameworks.
/// </summary>
public class RunTestsTool : ITool
{
    public string Name => "run_tests";
    public string Description => "Run tests using detected test framework (xUnit, NUnit, MSTest, Jest, pytest, etc.)";
    public ToolCategory Category => ToolCategory.Testing;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to project or test directory (default: current directory)"
                },
                "filter": {
                    "type": "string",
                    "description": "Test filter pattern (e.g., specific test name or category)"
                },
                "framework": {
                    "type": "string",
                    "description": "Test framework: dotnet, npm, pytest, cargo (auto-detected if not specified)"
                },
                "verbose": {
                    "type": "boolean",
                    "description": "Enable verbose output (default: false)"
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
            var filter = args.ContainsKey("filter") ? args["filter"].ToString() : null;
            var framework = args.ContainsKey("framework") ? args["framework"].ToString() : null;
            var verbose = args.ContainsKey("verbose") && Convert.ToBoolean(args["verbose"]);

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

            // Auto-detect framework if not specified
            if (string.IsNullOrEmpty(framework))
            {
                framework = DetectTestFramework(path);
            }

            if (string.IsNullOrEmpty(framework))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = "Could not detect test framework. Specify 'framework' parameter.",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            // Build command based on framework
            var (command, arguments) = BuildTestCommand(framework, path, filter, verbose);

            // Execute tests
            var processStartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = path,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var output = new StringBuilder();
            var error = new StringBuilder();

            using var process = new Process { StartInfo = processStartInfo };
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    output.AppendLine(e.Data);
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    error.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            stopwatch.Stop();

            var exitCode = process.ExitCode;
            var outputText = output.ToString();
            var errorText = error.ToString();

            // Parse test results
            var summary = ParseTestResults(outputText, framework);

            if (exitCode == 0)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = true,
                    Output = $"✓ Tests passed!\n\n{summary}\n\nOutput:\n{outputText}",
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
                    Error = $"✗ Tests failed!\n\n{summary}\n\nOutput:\n{outputText}\n\nError:\n{errorText}",
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
                Error = $"Error running tests: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    private string? DetectTestFramework(string path)
    {
        // Check for .NET projects
        if (Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories).Any())
            return "dotnet";

        // Check for Node.js projects
        if (System.IO.File.Exists(Path.Combine(path, "package.json")))
            return "npm";

        // Check for Python projects
        if (Directory.GetFiles(path, "*test*.py", SearchOption.AllDirectories).Any())
            return "pytest";

        // Check for Rust projects
        if (System.IO.File.Exists(Path.Combine(path, "Cargo.toml")))
            return "cargo";

        return null;
    }

    private (string command, string arguments) BuildTestCommand(string framework, string path, string? filter, bool verbose)
    {
        return framework.ToLower() switch
        {
            "dotnet" => ("dotnet", $"test{(filter != null ? $" --filter \"{filter}\"" : "")}{(verbose ? " -v detailed" : "")}"),
            "npm" => ("npm", $"test{(filter != null ? $" -- --testNamePattern=\"{filter}\"" : "")}"),
            "pytest" => ("pytest", $"{(filter != null ? $"-k \"{filter}\"" : "")}{(verbose ? " -v" : "")}"),
            "cargo" => ("cargo", $"test{(filter != null ? $" {filter}" : "")}{(verbose ? " --verbose" : "")}"),
            _ => ("dotnet", "test")
        };
    }

    private string ParseTestResults(string output, string framework)
    {
        // Simple parsing - can be enhanced
        var lines = output.Split('\n');
        var summary = new StringBuilder();

        foreach (var line in lines)
        {
            if (line.Contains("Passed:") || line.Contains("Failed:") || 
                line.Contains("Total:") || line.Contains("test") ||
                line.Contains("✓") || line.Contains("✗"))
            {
                summary.AppendLine(line.Trim());
            }
        }

        return summary.Length > 0 ? summary.ToString() : "Test execution completed";
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """run_tests({})""",
            """run_tests({"path": "/path/to/tests", "filter": "UserTests"})""",
            """run_tests({"framework": "dotnet", "verbose": true})"""
        };
    }
}
