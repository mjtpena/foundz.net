using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;

namespace Foundz.Net.Tools.File;

/// <summary>
/// Tool for copying files or directories.
/// </summary>
public class CopyFileTool : ITool
{
    public string Name => "copy_file";
    public string Description => "Copy a file or directory to a new location. Supports conflict resolution.";
    public ToolCategory Category => ToolCategory.FileOperations;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "sourcePath": {
                    "type": "string",
                    "description": "Path of the source file or directory"
                },
                "destinationPath": {
                    "type": "string",
                    "description": "Path for the destination"
                },
                "overwrite": {
                    "type": "boolean",
                    "description": "Whether to overwrite if destination exists",
                    "default": false
                },
                "recursive": {
                    "type": "boolean",
                    "description": "Whether to copy directories recursively",
                    "default": true
                }
            },
            "required": ["sourcePath", "destinationPath"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("sourcePath") || args["sourcePath"] is not string sourcePath || string.IsNullOrWhiteSpace(sourcePath))
            return Task.FromResult(false);
        
        if (!args.ContainsKey("destinationPath") || args["destinationPath"] is not string destPath || string.IsNullOrWhiteSpace(destPath))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var sourcePath = args["sourcePath"].ToString()!;
            var destinationPath = args["destinationPath"].ToString()!;
            var overwrite = args.ContainsKey("overwrite") && Convert.ToBoolean(args["overwrite"]);
            var recursive = !args.ContainsKey("recursive") || Convert.ToBoolean(args["recursive"]);

            if (!System.IO.File.Exists(sourcePath) && !Directory.Exists(sourcePath))
            {
                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Source not found: {sourcePath}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                });
            }

            var isDirectory = Directory.Exists(sourcePath);

            if (isDirectory)
            {
                CopyDirectory(sourcePath, destinationPath, recursive, overwrite);
            }
            else
            {
                var destDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                System.IO.File.Copy(sourcePath, destinationPath, overwrite);
            }

            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = $"Successfully copied {(isDirectory ? "directory" : "file")} from {sourcePath} to {destinationPath}",
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["sourcePath"] = sourcePath,
                    ["destinationPath"] = destinationPath,
                    ["isDirectory"] = isDirectory
                }
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = false,
                Error = ex.Message,
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            });
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir, bool recursive, bool overwrite)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        Directory.CreateDirectory(destDir);

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destDir, file.Name);
            file.CopyTo(targetFilePath, overwrite);
        }

        if (recursive)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                var newDestinationDir = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true, overwrite);
            }
        }
    }

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "copy_file({\"sourcePath\": \"file.txt\", \"destinationPath\": \"backup/file.txt\"})",
            "copy_file({\"sourcePath\": \"src\", \"destinationPath\": \"backup/src\", \"recursive\": true})"
        ];
    }
}
