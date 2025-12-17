using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using LibGit2Sharp;
using System.Diagnostics;
using System.Text;

namespace Foundz.Net.Tools.Git;

/// <summary>
/// Tool for managing git branches.
/// </summary>
public class GitBranchTool : ITool
{
    public string Name => "git_branch";
    public string Description => "List, create, or delete git branches";
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
                "action": {
                    "type": "string",
                    "description": "Action: list, create, delete (default: list)"
                },
                "branch_name": {
                    "type": "string",
                    "description": "Branch name (required for create/delete actions)"
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
            var action = args.ContainsKey("action") ? args["action"].ToString()!.ToLower() : "list";
            var branchName = args.ContainsKey("branch_name") ? args["branch_name"].ToString() : null;

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

            switch (action)
            {
                case "list":
                    output.AppendLine($"Branches in {Path.GetFileName(repo.Info.WorkingDirectory.TrimEnd('/'))}:");
                    output.AppendLine();

                    foreach (var branch in repo.Branches)
                    {
                        var current = branch.IsCurrentRepositoryHead ? "* " : "  ";
                        var tracking = branch.IsTracking ? $" -> {branch.TrackedBranch.FriendlyName}" : "";
                        var ahead = branch.TrackingDetails?.AheadBy > 0 ? $" [ahead {branch.TrackingDetails.AheadBy}]" : "";
                        var behind = branch.TrackingDetails?.BehindBy > 0 ? $" [behind {branch.TrackingDetails.BehindBy}]" : "";
                        
                        output.AppendLine($"{current}{branch.FriendlyName}{tracking}{ahead}{behind}");
                    }
                    break;

                case "create":
                    if (string.IsNullOrEmpty(branchName))
                    {
                        return Task.FromResult(new ToolResult
                        {
                            ToolCallId = string.Empty,
                            ToolName = Name,
                            Success = false,
                            Error = "branch_name is required for create action",
                            ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                        });
                    }

                    var newBranch = repo.CreateBranch(branchName);
                    output.AppendLine($"Created branch: {newBranch.FriendlyName}");
                    break;

                case "delete":
                    if (string.IsNullOrEmpty(branchName))
                    {
                        return Task.FromResult(new ToolResult
                        {
                            ToolCallId = string.Empty,
                            ToolName = Name,
                            Success = false,
                            Error = "branch_name is required for delete action",
                            ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                        });
                    }

                    var branchToDelete = repo.Branches[branchName];
                    if (branchToDelete == null)
                    {
                        return Task.FromResult(new ToolResult
                        {
                            ToolCallId = string.Empty,
                            ToolName = Name,
                            Success = false,
                            Error = $"Branch not found: {branchName}",
                            ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                        });
                    }

                    if (branchToDelete.IsCurrentRepositoryHead)
                    {
                        return Task.FromResult(new ToolResult
                        {
                            ToolCallId = string.Empty,
                            ToolName = Name,
                            Success = false,
                            Error = "Cannot delete the current branch",
                            ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                        });
                    }

                    repo.Branches.Remove(branchToDelete);
                    output.AppendLine($"Deleted branch: {branchName}");
                    break;

                default:
                    return Task.FromResult(new ToolResult
                    {
                        ToolCallId = string.Empty,
                        ToolName = Name,
                        Success = false,
                        Error = $"Unknown action: {action}. Valid actions: list, create, delete",
                        ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                    });
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
                Error = $"Error managing branches: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            });
        }
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """git_branch({"action": "list"})""",
            """git_branch({"action": "create", "branch_name": "feature/new-feature"})""",
            """git_branch({"action": "delete", "branch_name": "old-branch"})"""
        };
    }
}
