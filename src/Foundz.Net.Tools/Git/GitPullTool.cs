using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using LibGit2Sharp;
using System.Text;

namespace Foundz.Net.Tools.Git;

/// <summary>
/// Tool for git pull operations.
/// </summary>
public class GitPullTool : ITool
{
    public string Name => "git_pull";
    public string Description => "Pull changes from remote repository with optional rebase.";
    public ToolCategory Category => ToolCategory.GitOperations;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Warning;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "remote": {
                    "type": "string",
                    "description": "Name of the remote (default: origin)",
                    "default": "origin"
                },
                "rebase": {
                    "type": "boolean",
                    "description": "Use rebase instead of merge",
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
            var remote = args.ContainsKey("remote") ? args["remote"].ToString() : "origin";
            var rebase = args.ContainsKey("rebase") && Convert.ToBoolean(args["rebase"]);
            var repoPath = args.ContainsKey("repositoryPath") ? args["repositoryPath"].ToString() : Directory.GetCurrentDirectory();

            using var repo = new Repository(repoPath);

            var signature = repo.Config.BuildSignature(DateTimeOffset.Now);
            var options = new PullOptions
            {
                FetchOptions = new FetchOptions()
            };

            if (rebase)
            {
                options.MergeOptions = new MergeOptions
                {
                    FastForwardStrategy = FastForwardStrategy.FastForwardOnly
                };
            }

            var result = Commands.Pull(repo, signature, options);

            var output = new StringBuilder();
            output.AppendLine($"Pull Status: {result.Status}");
            
            if (result.Commit != null)
            {
                output.AppendLine($"Merged commit: {result.Commit.Sha[..7]}");
                output.AppendLine($"Author: {result.Commit.Author.Name}");
                output.AppendLine($"Message: {result.Commit.MessageShort}");
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
                    ["status"] = result.Status.ToString(),
                    ["remote"] = remote!,
                    ["rebase"] = rebase
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
            "git_pull({})",
            "git_pull({\"rebase\": true})",
            "git_pull({\"remote\": \"upstream\"})"
        ];
    }
}
