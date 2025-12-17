using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using LibGit2Sharp;
using System.Text;

namespace Foundz.Net.Tools.Git;

/// <summary>
/// Tool for checking git repository status.
/// </summary>
public class GitStatusTool : ITool
{
    public string Name => "git_status";
    public string Description => "Get the status of the git repository, including modified, staged, and untracked files.";
    public ToolCategory Category => ToolCategory.GitOperations;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to the git repository (defaults to current directory)"
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
        var startTime = DateTime.UtcNow;

        try
        {
            var path = args.ContainsKey("path") ? args["path"].ToString() : ".";
            if (string.IsNullOrWhiteSpace(path))
                path = ".";

            var repoPath = Repository.Discover(path);
            if (repoPath == null)
            {
                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = "Not a git repository",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                });
            }

            using var repo = new Repository(repoPath);
            var status = repo.RetrieveStatus();
            var output = new StringBuilder();

            // Branch information
            var branch = repo.Head;
            output.AppendLine($"Branch: {branch.FriendlyName}");
            
            if (branch.IsTracking)
            {
                output.AppendLine($"Tracking: {branch.TrackedBranch.FriendlyName}");
                output.AppendLine($"Ahead: {branch.TrackingDetails.AheadBy}, Behind: {branch.TrackingDetails.BehindBy}");
            }
            
            output.AppendLine();

            // Staged changes
            var staged = status.Where(s => s.State.HasFlag(FileStatus.ModifiedInIndex) || 
                                          s.State.HasFlag(FileStatus.NewInIndex) ||
                                          s.State.HasFlag(FileStatus.RenamedInIndex) ||
                                          s.State.HasFlag(FileStatus.DeletedFromIndex)).ToList();
            
            if (staged.Any())
            {
                output.AppendLine($"Staged Changes ({staged.Count}):");
                foreach (var item in staged)
                {
                    var statusChar = GetStatusChar(item.State);
                    output.AppendLine($"  {statusChar} {item.FilePath}");
                }
                output.AppendLine();
            }

            // Modified files
            var modified = status.Where(s => s.State.HasFlag(FileStatus.ModifiedInWorkdir)).ToList();
            if (modified.Any())
            {
                output.AppendLine($"Modified Files ({modified.Count}):");
                foreach (var item in modified)
                {
                    output.AppendLine($"  M {item.FilePath}");
                }
                output.AppendLine();
            }

            // Untracked files
            var untracked = status.Untracked.ToList();
            if (untracked.Any())
            {
                output.AppendLine($"Untracked Files ({untracked.Count}):");
                foreach (var item in untracked.Take(20))
                {
                    output.AppendLine($"  ? {item.FilePath}");
                }
                if (untracked.Count > 20)
                {
                    output.AppendLine($"  ... and {untracked.Count - 20} more");
                }
                output.AppendLine();
            }

            // Deleted files
            var deleted = status.Where(s => s.State.HasFlag(FileStatus.DeletedFromWorkdir)).ToList();
            if (deleted.Any())
            {
                output.AppendLine($"Deleted Files ({deleted.Count}):");
                foreach (var item in deleted)
                {
                    output.AppendLine($"  D {item.FilePath}");
                }
                output.AppendLine();
            }

            if (!status.IsDirty)
            {
                output.AppendLine("Working directory is clean ✓");
            }

            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = output.ToString(),
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["isDirty"] = status.IsDirty,
                    ["stagedCount"] = staged.Count,
                    ["modifiedCount"] = modified.Count,
                    ["untrackedCount"] = untracked.Count
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

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "git_status({})",
            "git_status({\"path\": \"..\"})"
        ];
    }

    private static string GetStatusChar(FileStatus status)
    {
        if (status.HasFlag(FileStatus.NewInIndex)) return "A";
        if (status.HasFlag(FileStatus.ModifiedInIndex)) return "M";
        if (status.HasFlag(FileStatus.DeletedFromIndex)) return "D";
        if (status.HasFlag(FileStatus.RenamedInIndex)) return "R";
        return "?";
    }
}
