using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using LibGit2Sharp;
using System.Diagnostics;

namespace Foundz.Net.Tools.Git;

/// <summary>
/// Tool for staging files in git.
/// </summary>
public class GitAddTool : ITool
{
    public string Name => "git_add";
    public string Description => "Stage files for commit in git repository";
    public ToolCategory Category => ToolCategory.GitOperations;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to git repository (default: current directory)"
                },
                "files": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Files to stage (use '.' for all files)"
                }
            },
            "required": ["files"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(args.ContainsKey("files"));
    }

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var repoPath = args.ContainsKey("path") ? args["path"].ToString()! : Directory.GetCurrentDirectory();
            var files = args["files"] as IEnumerable<object> ?? Array.Empty<object>();

            var repoRoot = Repository.Discover(repoPath);
            if (repoRoot == null)
            {
                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = "Not a git repository",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                });
            }

            using var repo = new Repository(repoRoot);
            var stagedCount = 0;

            foreach (var file in files)
            {
                var filePath = file.ToString()!;
                
                if (filePath == ".")
                {
                    // Stage all changes
                    Commands.Stage(repo, "*");
                    stagedCount = repo.RetrieveStatus().Staged.Count();
                    break;
                }
                else
                {
                    Commands.Stage(repo, filePath);
                    stagedCount++;
                }
            }

            stopwatch.Stop();

            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = $"Staged {stagedCount} file(s)",
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
                Error = $"Error staging files: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            });
        }
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """git_add({"files": ["."]})""",
            """git_add({"files": ["src/Program.cs", "README.md"]})"""
        };
    }
}
