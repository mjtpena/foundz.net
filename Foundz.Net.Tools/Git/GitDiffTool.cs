using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using LibGit2Sharp;
using System.Diagnostics;
using System.Text;

namespace Foundz.Net.Tools.Git;

/// <summary>
/// Tool for showing git diff.
/// </summary>
public class GitDiffTool : ITool
{
    public string Name => "git_diff";
    public string Description => "Show git diff for working directory or staged changes";
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
                "staged": {
                    "type": "boolean",
                    "description": "Show staged changes (default: false, shows working directory changes)"
                },
                "file_path": {
                    "type": "string",
                    "description": "Optional specific file path to show diff for"
                }
            }
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var repoPath = args.ContainsKey("path") ? args["path"].ToString()! : Directory.GetCurrentDirectory();
            var staged = args.ContainsKey("staged") && Convert.ToBoolean(args["staged"]);
            var filePath = args.ContainsKey("file_path") ? args["file_path"].ToString() : null;

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
            var output = new StringBuilder();

            if (staged)
            {
                // Diff between HEAD and index (staged changes)
                var headTree = repo.Head.Tip?.Tree;
                var changes = repo.Diff.Compare<TreeChanges>(headTree, DiffTargets.Index);

                if (!changes.Any())
                {
                    output.AppendLine("No staged changes.");
                }
                else
                {
                    output.AppendLine($"Staged changes ({changes.Count()} file(s)):");
                    output.AppendLine();

                    foreach (var change in changes)
                    {
                        if (filePath != null && !change.Path.Contains(filePath))
                            continue;

                        output.AppendLine($"[{GetChangeSymbol(change.Status)}] {change.Path}");
                        
                        if (change.Status != ChangeKind.Deleted)
                        {
                            var patch = repo.Diff.Compare<Patch>(headTree, DiffTargets.Index, new[] { change.Path });
                            if (patch != null)
                            {
                                output.AppendLine(patch[change.Path]?.Patch ?? "");
                            }
                        }
                        output.AppendLine();
                    }
                }
            }
            else
            {
                // Diff between index and working directory
                var changes = repo.Diff.Compare<TreeChanges>();

                if (!changes.Any())
                {
                    output.AppendLine("No changes in working directory.");
                }
                else
                {
                    output.AppendLine($"Working directory changes ({changes.Count()} file(s)):");
                    output.AppendLine();

                    foreach (var change in changes)
                    {
                        if (filePath != null && !change.Path.Contains(filePath))
                            continue;

                        output.AppendLine($"[{GetChangeSymbol(change.Status)}] {change.Path}");
                        
                        var patch = repo.Diff.Compare<Patch>(new[] { change.Path });
                        if (patch != null)
                        {
                            output.AppendLine(patch[change.Path]?.Patch ?? "");
                        }
                        output.AppendLine();
                    }
                }
            }

            stopwatch.Stop();

            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = output.ToString(),
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
                Error = $"Error getting diff: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            });
        }
    }

    private static string GetChangeSymbol(ChangeKind status)
    {
        return status switch
        {
            ChangeKind.Added => "A",
            ChangeKind.Modified => "M",
            ChangeKind.Deleted => "D",
            ChangeKind.Renamed => "R",
            _ => "?"
        };
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """git_diff({})""",
            """git_diff({"staged": true})""",
            """git_diff({"file_path": "README.md"})"""
        };
    }
}
