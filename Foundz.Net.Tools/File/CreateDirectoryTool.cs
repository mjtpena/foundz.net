using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Diagnostics;

namespace Foundz.Net.Tools.File;

/// <summary>
/// Tool for creating directories.
/// </summary>
public class CreateDirectoryTool : ITool
{
    public string Name => "create_directory";
    public string Description => "Create a new directory, optionally creating parent directories";
    public ToolCategory Category => ToolCategory.FileOperations;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to the directory to create"
                },
                "create_parents": {
                    "type": "boolean",
                    "description": "Create parent directories if they don't exist (default: true)"
                }
            },
            "required": ["path"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(args.ContainsKey("path"));
    }

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var path = args["path"].ToString()!;
            var createParents = !args.ContainsKey("create_parents") || Convert.ToBoolean(args["create_parents"]);

            if (Directory.Exists(path))
            {
                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = true,
                    Output = $"Directory already exists: {path}",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                });
            }

            if (createParents)
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                var parentDir = Path.GetDirectoryName(path);
                if (parentDir != null && !Directory.Exists(parentDir))
                {
                    return Task.FromResult(new ToolResult
                    {
                        ToolCallId = string.Empty,
                        ToolName = Name,
                        Success = false,
                        Error = $"Parent directory does not exist: {parentDir}. Set create_parents to true.",
                        ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                    });
                }

                Directory.CreateDirectory(path);
            }

            stopwatch.Stop();

            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = $"Created directory: {Path.GetFullPath(path)}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = false,
                Error = $"Error creating directory: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            });
        }
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """create_directory({"path": "new_folder"})""",
            """create_directory({"path": "parent/child/grandchild", "create_parents": true})"""
        };
    }
}
