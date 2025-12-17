using Microsoft.Agents.Core;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.Logging;
using Foundz.Net.Shared.Models;
using Foundz.Net.Shared.Interfaces;

namespace Foundz.Net.Core.AI;

/// <summary>
/// Integration with Microsoft Agent Framework for advanced agentic capabilities.
/// Provides enterprise-grade agent orchestration, memory, and tool execution.
/// </summary>
public class MicrosoftAgentFrameworkIntegration
{
    private readonly ILogger<MicrosoftAgentFrameworkIntegration> _logger;
    private readonly AgentClient _agentClient;
    private readonly string _agentId;

    public MicrosoftAgentFrameworkIntegration(
        string endpoint,
        string apiKey,
        string agentId,
        ILogger<MicrosoftAgentFrameworkIntegration> logger)
    {
        _logger = logger;
        _agentId = agentId;
        
        // Initialize Microsoft Agent Framework client
        _agentClient = new AgentClient(endpoint, new ApiKeyCredential(apiKey));
        
        _logger.LogInformation(
            "Initialized Microsoft Agent Framework integration for agent {AgentId}",
            agentId);
    }

    /// <summary>
    /// Creates or updates an agent with the Microsoft Agent Framework.
    /// </summary>
    public async Task<AgentInfo> CreateOrUpdateAgentAsync(
        string name,
        string description,
        string instructions,
        List<ITool> tools,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating/updating agent {Name} in Microsoft Agent Framework", name);

            // Convert tools to Agent Framework format
            var agentTools = tools.Select(tool => new AgentTool
            {
                Type = "function",
                Function = new AgentFunction
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    Parameters = ConvertParameters(tool.Parameters)
                }
            }).ToList();

            // Create agent configuration
            var agentConfig = new AgentConfiguration
            {
                Name = name,
                Description = description,
                Instructions = instructions,
                Tools = agentTools,
                Model = "gpt-4-turbo", // Default model
                Temperature = 0.7f,
                TopP = 1.0f
            };

            // Create or update the agent
            var agent = await _agentClient.CreateOrUpdateAgentAsync(
                _agentId,
                agentConfig,
                cancellationToken);

            _logger.LogInformation("Agent {AgentId} created/updated successfully", agent.Id);

            return new AgentInfo
            {
                Id = agent.Id,
                Name = agent.Name,
                Description = agent.Description,
                CreatedAt = agent.CreatedAt,
                Model = agent.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create/update agent in Microsoft Agent Framework");
            throw;
        }
    }

    /// <summary>
    /// Runs an agent conversation using Microsoft Agent Framework.
    /// </summary>
    public async Task<AgentRunResult> RunAgentAsync(
        string threadId,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Running agent {AgentId} on thread {ThreadId}",
                _agentId,
                threadId);

            // Add message to thread
            await _agentClient.AddMessageToThreadAsync(
                threadId,
                new ThreadMessage
                {
                    Role = "user",
                    Content = message
                },
                cancellationToken);

            // Run the agent
            var run = await _agentClient.RunAgentAsync(
                threadId,
                _agentId,
                cancellationToken);

            // Wait for completion
            while (run.Status == "in_progress" || run.Status == "queued")
            {
                await Task.Delay(1000, cancellationToken);
                run = await _agentClient.GetRunAsync(threadId, run.Id, cancellationToken);
            }

            // Get the assistant's response
            var messages = await _agentClient.GetThreadMessagesAsync(
                threadId,
                limit: 1,
                cancellationToken: cancellationToken);

            var lastMessage = messages.FirstOrDefault();

            return new AgentRunResult
            {
                RunId = run.Id,
                Status = run.Status,
                Response = lastMessage?.Content ?? string.Empty,
                ToolCalls = run.RequiredActions?.SubmitToolOutputs?.ToolCalls?
                    .Select(tc => new ToolCallInfo
                    {
                        Id = tc.Id,
                        Name = tc.Function.Name,
                        Arguments = tc.Function.Arguments
                    })
                    .ToList() ?? new List<ToolCallInfo>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run agent in Microsoft Agent Framework");
            throw;
        }
    }

    /// <summary>
    /// Submits tool outputs back to the agent run.
    /// </summary>
    public async Task SubmitToolOutputsAsync(
        string threadId,
        string runId,
        List<ToolOutput> toolOutputs,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Submitting {Count} tool outputs for run {RunId}",
                toolOutputs.Count,
                runId);

            var outputs = toolOutputs.Select(to => new AgentToolOutput
            {
                ToolCallId = to.ToolCallId,
                Output = to.Output
            }).ToList();

            await _agentClient.SubmitToolOutputsAsync(
                threadId,
                runId,
                outputs,
                cancellationToken);

            _logger.LogInformation("Tool outputs submitted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit tool outputs");
            throw;
        }
    }

    /// <summary>
    /// Creates a new thread for agent conversations.
    /// </summary>
    public async Task<string> CreateThreadAsync(
        string? initialMessage = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var thread = await _agentClient.CreateThreadAsync(
                messages: initialMessage != null ? new[]
                {
                    new ThreadMessage
                    {
                        Role = "user",
                        Content = initialMessage
                    }
                } : null,
                metadata: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Created thread {ThreadId}", thread.Id);
            return thread.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create thread");
            throw;
        }
    }

    /// <summary>
    /// Gets thread messages.
    /// </summary>
    public async Task<List<ThreadMessageInfo>> GetThreadMessagesAsync(
        string threadId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _agentClient.GetThreadMessagesAsync(
                threadId,
                limit: limit,
                cancellationToken: cancellationToken);

            return messages.Select(m => new ThreadMessageInfo
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get thread messages");
            throw;
        }
    }

    private object ConvertParameters(Dictionary<string, object> parameters)
    {
        // Convert tool parameters to Agent Framework format
        return new
        {
            type = "object",
            properties = parameters,
            required = parameters.Keys.ToList()
        };
    }
}

#region Supporting Models

public class AgentInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public required string Model { get; init; }
}

public class AgentRunResult
{
    public required string RunId { get; init; }
    public required string Status { get; init; }
    public required string Response { get; init; }
    public required List<ToolCallInfo> ToolCalls { get; init; }
}

public class ToolCallInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Arguments { get; init; }
}

public class ToolOutput
{
    public required string ToolCallId { get; init; }
    public required string Output { get; init; }
}

public class ThreadMessageInfo
{
    public required string Id { get; init; }
    public required string Role { get; init; }
    public required string Content { get; init; }
    public DateTime CreatedAt { get; init; }
}

#endregion
