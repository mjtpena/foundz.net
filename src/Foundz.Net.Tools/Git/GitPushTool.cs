using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using LibGit2Sharp;

namespace Foundz.Net.Tools.Git;

/// <summary>
/// Tool for git push operations.
/// </summary>
public class GitPushTool : ITool
{
    public string Name => "git_push";
    public string Description => "Push commits to remote repository with safety checks.";
    public ToolCategory Category => ToolCategory.GitOperations;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Danger;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "remote": {
                    "type": "string",
                    "description": "Name of the remote (default: origin)",
                    "default": "origin"
                },
                "branch": {
                    "type": "string",
                    "description": "Branch name to push (defaults to current branch)"
                },
                "force": {
                    "type": "boolean",
                    "description": "Force push (dangerous!)",
                    "default": false
                },
                "setUpstream": {
                    "type": "boolean",
                    "description": "Set upstream tracking branch",
                    "default": false
                },
                "repositoryPath": {
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
            var remoteName = args.ContainsKey("remote") ? args["remote"].ToString() : "origin";
            var branchName = args.ContainsKey("branch") ? args["branch"].ToString() : null;
            var force = args.ContainsKey("force") && Convert.ToBoolean(args["force"]);
            var setUpstream = args.ContainsKey("setUpstream") && Convert.ToBoolean(args["setUpstream"]);
            var repoPath = args.ContainsKey("repositoryPath") ? args["repositoryPath"].ToString() : Directory.GetCurrentDirectory();

            using var repo = new Repository(repoPath);

            var currentBranch = repo.Head;
            var branch = string.IsNullOrEmpty(branchName) ? currentBranch : repo.Branches[branchName];

            if (branch == null)
            {
                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Branch not found: {branchName}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                });
            }

            var remote = repo.Network.Remotes[remoteName];
            if (remote == null)
            {
                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Remote not found: {remoteName}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                });
            }

            var pushOptions = new PushOptions();
            
            var refSpec = $"refs/heads/{branch.FriendlyName}:refs/heads/{branch.FriendlyName}";
            if (force)
            {
                refSpec = $"+{refSpec}";
            }

            repo.Network.Push(remote, refSpec, pushOptions);

            if (setUpstream && !force)
            {
                repo.Branches.Update(branch, b => b.Remote = remoteName, 
                    b => b.UpstreamBranch = branch.CanonicalName);
            }

            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = $"Successfully pushed {branch.FriendlyName} to {remoteName}" + 
                         (force ? " (forced)" : "") +
                         (setUpstream ? " and set upstream" : ""),
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["remote"] = remoteName!,
                    ["branch"] = branch.FriendlyName,
                    ["force"] = force,
                    ["setUpstream"] = setUpstream
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
            "git_push({})",
            "git_push({\"setUpstream\": true})",
            "git_push({\"branch\": \"feature/new\", \"setUpstream\": true})"
        ];
    }
}
