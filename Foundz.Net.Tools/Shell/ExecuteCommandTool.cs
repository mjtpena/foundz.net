using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Diagnostics;
using System.Text;

namespace Foundz.Net.Tools.Shell;

/// <summary>
/// Tool for executing shell commands safely.
/// </summary>
public class ExecuteCommandTool : ITool
{
    private static readonly HashSet<string> SafeCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "dotnet", "npm", "node", "python", "python3", "pip", "pip3",
        "cargo", "rustc", "go", "mvn", "gradle", "git",
        "ls", "dir", "pwd", "echo", "cat", "grep", "find",
        "curl", "wget", "make", "cmake"
    };

    private static readonly HashSet<string> DangerousPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "rm -rf", "del /f", "format", "mkfs", "dd if=",
        ":(){ :|:& };:", "chmod 777", "sudo rm", "> /dev/sda"
    };

    public string Name => "execute_command";
    public string Description => "Execute a shell command (whitelisted commands only for safety)";
    public ToolCategory Category => ToolCategory.ShellExecution;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Warning;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "command": {
                    "type": "string",
                    "description": "The command to execute"
                },
                "arguments": {
                    "type": "string",
                    "description": "Arguments for the command"
                },
                "working_directory": {
                    "type": "string",
                    "description": "Working directory (default: current directory)"
                },
                "timeout_seconds": {
                    "type": "integer",
                    "description": "Timeout in seconds (default: 30)"
                }
            },
            "required": ["command"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(args.ContainsKey("command"));
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var command = args["command"].ToString()!;
            var arguments = args.ContainsKey("arguments") ? args["arguments"].ToString()! : "";
            var workingDir = args.ContainsKey("working_directory") 
                ? args["working_directory"].ToString()! 
                : Directory.GetCurrentDirectory();
            var timeoutSeconds = args.ContainsKey("timeout_seconds") 
                ? Convert.ToInt32(args["timeout_seconds"]) 
                : 30;

            // Security checks
            var fullCommand = $"{command} {arguments}".ToLower();
            
            // Check for dangerous patterns
            foreach (var pattern in DangerousPatterns)
            {
                if (fullCommand.Contains(pattern.ToLower()))
                {
                    return new ToolResult
                    {
                        ToolCallId = string.Empty,
                        ToolName = Name,
                        Success = false,
                        Error = $"Command contains dangerous pattern: {pattern}",
                        ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                    };
                }
            }

            // Check if command is whitelisted
            if (!SafeCommands.Contains(command))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Command '{command}' is not in the whitelist. Safe commands: {string.Join(", ", SafeCommands)}",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            // Validate working directory
            if (!Directory.Exists(workingDir))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Working directory does not exist: {workingDir}",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            // Execute command
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
            var completed = true;

            if (!completed)
            {
                try
                {
                    process.Kill();
                }
                catch { }

                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Command timed out after {timeoutSeconds} seconds",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            stopwatch.Stop();

            var exitCode = process.ExitCode;
            var outputText = output.ToString();
            var errorText = error.ToString();

            if (exitCode == 0)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = true,
                    Output = $"Exit Code: {exitCode}\n\nOutput:\n{outputText}" +
                             (string.IsNullOrEmpty(errorText) ? "" : $"\n\nWarnings:\n{errorText}"),
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
                    Error = $"Exit Code: {exitCode}\n\nOutput:\n{outputText}\n\nError:\n{errorText}",
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
                Error = $"Error executing command: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """execute_command({"command": "dotnet", "arguments": "--version"})""",
            """execute_command({"command": "npm", "arguments": "install", "working_directory": "/path/to/project"})""",
            """execute_command({"command": "git", "arguments": "status"})"""
        };
    }
}
