using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using LibGit2Sharp;

namespace Foundz.Net.Tools.Git;

/// <summary>
/// Tool for git checkout operations.
/// </summary>
public class GitCheckoutTool : ITool
{
    public string Name => "git_checkout";
    public string Description => "Switch branches, create and switch to new branch, or checkout specific files.";
    public ToolCategory Category => ToolCategory.GitOperations;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Warning;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "branchName": {
                    "type": "string",
                    "description": "Name of the branch to checkout"
                },
                "createNew": {
                    "type": "boolean",
                    "description": "Create a new branch if it doesn't exist",
                    "default": false
                },
                "filePath": {
                    "type": "string",
                    "description": "Optional: Specific file path to checkout (revert changes)"
                },
                "repositoryPath": {
                    "type": "string",
                    "description": "Path to the git repository (defaults to current directory)"
                }
            },
            "required": ["branchName"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("branchName") || args["branchName"] is not string branch || string.IsNullOrWhiteSpace(branch))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var branchName = args["branchName"].ToString()!;
            var createNew = args.ContainsKey("createNew") && Convert.ToBoolean(args["createNew"]);
            var filePath = args.ContainsKey("filePath") ? args["filePath"].ToString() : null;
            var repoPath = args.ContainsKey("repositoryPath") ? args["repositoryPath"].ToString() : Directory.GetCurrentDirectory();

            using var repo = new Repository(repoPath);

            if (!string.IsNullOrEmpty(filePath))
            {
                // Checkout specific file
                Commands.Checkout(repo, filePath);
                
                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = true,
                    Output = $"Checked out file: {filePath}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                });
            }

            var branch = repo.Branches[branchName];

            if (branch == null)
            {
                if (createNew)
                {
                    branch = repo.CreateBranch(branchName);
                    Commands.Checkout(repo, branch);
                    
                    return Task.FromResult(new ToolResult
                    {
                        ToolCallId = string.Empty,
                        ToolName = Name,
                        Success = true,
                        Output = $"Created and checked out new branch: {branchName}",
                        ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                        Metadata = new Dictionary<string, object>
                        {
                            ["branchName"] = branchName,
                            ["created"] = true
                        }
                    });
                }
                else
                {
                    return Task.FromResult(new ToolResult
                    {
                        ToolCallId = string.Empty,
                        ToolName = Name,
                        Success = false,
                        Error = $"Branch not found: {branchName}. Use createNew=true to create it.",
                        ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                    });
                }
            }

            Commands.Checkout(repo, branch);

            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = $"Switched to branch: {branchName}",
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["branchName"] = branchName,
                    ["created"] = false
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
            "git_checkout({\"branchName\": \"main\"})",
            "git_checkout({\"branchName\": \"feature/new-feature\", \"createNew\": true})",
            "git_checkout({\"branchName\": \"HEAD\", \"filePath\": \"Program.cs\"})"
        ];
    }
}
