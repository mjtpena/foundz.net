using System.Runtime.CompilerServices;
using Foundz.Net.Core.AI;
using Foundz.Net.Core.ToolRegistry;
using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.Orchestration;

/// <summary>
/// Production-grade agent orchestrator implementing the agentic loop with tool execution.
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IAIClient _aiClient;
    private readonly IToolRegistry _toolRegistry;
    private readonly ToolExecutor _toolExecutor;
    private readonly ToolCallParser _toolCallParser;
    private readonly ToolResultFormatter _resultFormatter;
    private readonly ContextManager _contextManager;
    private readonly CostTracker? _costTracker;
    private readonly RequestMetricsCollector? _metricsCollector;
    private readonly ILogger<AgentOrchestrator> _logger;
    
    private readonly int _maxIterations;
    private readonly string _systemPrompt;

    public AgentOrchestrator(
        IAIClient aiClient,
        IToolRegistry toolRegistry,
        ToolExecutor toolExecutor,
        ToolCallParser toolCallParser,
        ToolResultFormatter resultFormatter,
        ContextManager contextManager,
        ILogger<AgentOrchestrator> logger,
        CostTracker? costTracker = null,
        RequestMetricsCollector? metricsCollector = null,
        int maxIterations = 15,
        string? systemPrompt = null)
    {
        _aiClient = aiClient;
        _toolRegistry = toolRegistry;
        _toolExecutor = toolExecutor;
        _toolCallParser = toolCallParser;
        _resultFormatter = resultFormatter;
        _contextManager = contextManager;
        _costTracker = costTracker;
        _metricsCollector = metricsCollector;
        _logger = logger;
        _maxIterations = maxIterations;
        _systemPrompt = systemPrompt ?? BuildDefaultSystemPrompt();
    }

    /// <summary>
    /// Processes a user message through the agentic loop.
    /// </summary>
    public async Task<AgentResponse> ProcessMessageAsync(
        string userMessage,
        Foundz.Net.Shared.Models.Session session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting agent orchestration for session {SessionId}", session.Id);

        var allToolResults = new List<ToolResult>();
        var totalTokenUsage = new TokenUsage { PromptTokens = 0, CompletionTokens = 0 };
        var iterationCount = 0;
        var finalContent = string.Empty;

        try
        {
            // Build initial context
            var messages = _contextManager.BuildContext(session.Messages, _systemPrompt);
            
            // Add user message
            messages.Add(new Message
            {
                Role = MessageRole.User,
                Content = userMessage
            });

            // Get tool schemas
            var toolSchemas = _toolRegistry.GetToolSchemas();

            // Agentic loop
            while (iterationCount < _maxIterations)
            {
                iterationCount++;
                _logger.LogDebug("Iteration {Iteration} of {Max}", iterationCount, _maxIterations);

                // Call AI with current context and track metrics
                AIResponse aiResponse;
                using (var metricsTracker = _metricsCollector?.StartRequest("ai-model", session.Id.ToString()))
                {
                    try
                    {
                        aiResponse = await _aiClient.SendMessageAsync(
                            messages,
                            toolSchemas,
                            cancellationToken);

                        metricsTracker?.Complete(
                            aiResponse.TokenUsage.PromptTokens,
                            aiResponse.TokenUsage.CompletionTokens);
                    }
                    catch (Exception ex)
                    {
                        metricsTracker?.Fail(ex.Message);
                        throw;
                    }
                }

                // Track cost for this request
                _costTracker?.RecordRequest(
                    sessionId: session.Id.ToString(),
                    modelName: aiResponse.ModelUsed ?? "unknown",
                    promptTokens: aiResponse.TokenUsage.PromptTokens,
                    completionTokens: aiResponse.TokenUsage.CompletionTokens,
                    timestamp: DateTime.UtcNow);

                // Accumulate token usage
                totalTokenUsage = new TokenUsage
                {
                    PromptTokens = totalTokenUsage.PromptTokens + aiResponse.TokenUsage.PromptTokens,
                    CompletionTokens = totalTokenUsage.CompletionTokens + aiResponse.TokenUsage.CompletionTokens
                };

                // Store AI response content
                if (!string.IsNullOrWhiteSpace(aiResponse.Content))
                {
                    finalContent = aiResponse.Content;
                    messages.Add(new Message
                    {
                        Role = MessageRole.Assistant,
                        Content = aiResponse.Content
                    });
                }

                // Check for tool calls
                var toolCalls = _toolCallParser.ParseToolCalls(aiResponse);
                
                if (toolCalls.Count == 0)
                {
                    // No tool calls, we're done
                    _logger.LogInformation(
                        "Agent completed in {Iterations} iteration(s), no more tool calls",
                        iterationCount);
                    break;
                }

                _logger.LogInformation("AI requested {Count} tool call(s)", toolCalls.Count);

                // Execute tools
                var toolResults = await ExecuteToolsAsync(toolCalls, cancellationToken);
                allToolResults.AddRange(toolResults);

                // Add tool results to conversation
                var toolMessages = _resultFormatter.FormatAsMessages(toolResults);
                messages.AddRange(toolMessages);

                // Check if any critical tool failed
                if (toolResults.Any(r => !r.Success))
                {
                    _logger.LogWarning("Some tools failed, continuing with results");
                }

                // Check iteration limit
                if (iterationCount >= _maxIterations)
                {
                    _logger.LogWarning(
                        "Reached maximum iterations ({Max}), stopping agentic loop",
                        _maxIterations);
                    finalContent += "\n\n⚠️ Note: Reached maximum iteration limit. Task may be incomplete.";
                    break;
                }
            }

            return new AgentResponse
            {
                Content = finalContent,
                ToolResults = allToolResults,
                TokenUsage = totalTokenUsage,
                IterationCount = iterationCount,
                HitMaxIterations = iterationCount >= _maxIterations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during agent orchestration");
            throw;
        }
    }

    /// <summary>
    /// Processes a user message with streaming.
    /// </summary>
    public async IAsyncEnumerable<AgentEvent> ProcessMessageStreamAsync(
        string userMessage,
        Foundz.Net.Shared.Models.Session session,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting streaming agent orchestration for session {SessionId}", session.Id);

        // Build initial context
        var messages = _contextManager.BuildContext(session.Messages, _systemPrompt);
        
        messages.Add(new Message
        {
            Role = MessageRole.User,
            Content = userMessage
        });

        var toolSchemas = _toolRegistry.GetToolSchemas();
        var allToolResults = new List<ToolResult>();
        var iterationCount = 0;

        yield return new AgentEvent
        {
            Type = AgentEventType.StateChanged,
            State = AgentState.Thinking
        };

        // Agentic loop
        while (iterationCount < _maxIterations)
        {
            iterationCount++;
            
            yield return new AgentEvent
            {
                Type = AgentEventType.StateChanged,
                State = AgentState.Streaming
            };

            // Stream AI response
            var contentBuilder = new System.Text.StringBuilder();
            var toolCalls = new List<ToolCall>();

            await foreach (var chunk in _aiClient.StreamMessageAsync(messages, toolSchemas, cancellationToken))
            {
                if (!string.IsNullOrWhiteSpace(chunk.ContentDelta))
                {
                    contentBuilder.Append(chunk.ContentDelta);
                    yield return new AgentEvent
                    {
                        Type = AgentEventType.ContentDelta,
                        Content = chunk.ContentDelta
                    };
                }

                if (chunk.ToolCalls != null && chunk.ToolCalls.Count > 0)
                {
                    toolCalls.AddRange(chunk.ToolCalls);
                }
            }

            // Add assistant message to history
            var assistantContent = contentBuilder.ToString();
            if (!string.IsNullOrWhiteSpace(assistantContent))
            {
                messages.Add(new Message
                {
                    Role = MessageRole.Assistant,
                    Content = assistantContent
                });
            }

            // Check for tool calls
            if (toolCalls.Count == 0)
            {
                yield return new AgentEvent
                {
                    Type = AgentEventType.StateChanged,
                    State = AgentState.Completed
                };
                yield break;
            }

            // Execute tools
            yield return new AgentEvent
            {
                Type = AgentEventType.StateChanged,
                State = AgentState.ToolExecution
            };

            foreach (var toolCall in toolCalls)
            {
                yield return new AgentEvent
                {
                    Type = AgentEventType.ToolCallStarted,
                    ToolCall = toolCall
                };

                var result = await _toolExecutor.ExecuteAsync(toolCall, cancellationToken);
                allToolResults.Add(result);

                yield return new AgentEvent
                {
                    Type = AgentEventType.ToolCallCompleted,
                    ToolResult = result
                };
            }

            // Add tool results to conversation
            var toolMessages = _resultFormatter.FormatAsMessages(allToolResults);
            messages.AddRange(toolMessages);

            yield return new AgentEvent
            {
                Type = AgentEventType.IterationCompleted
            };

            if (iterationCount >= _maxIterations)
            {
                _logger.LogWarning("Reached maximum iterations ({Max})", _maxIterations);
                yield return new AgentEvent
                {
                    Type = AgentEventType.Error,
                    Content = "Reached maximum iteration limit"
                };
                yield break;
            }
        }

        yield return new AgentEvent
        {
            Type = AgentEventType.StateChanged,
            State = AgentState.Completed
        };
    }

    /// <summary>
    /// Executes tools, deciding between parallel or sequential execution.
    /// </summary>
    private async Task<List<ToolResult>> ExecuteToolsAsync(
        List<ToolCall> toolCalls,
        CancellationToken cancellationToken)
    {
        if (toolCalls.Count == 0)
            return new List<ToolResult>();

        // Determine execution strategy
        if (_toolExecutor.CanExecuteInParallel(toolCalls))
        {
            _logger.LogInformation("Executing {Count} tools in parallel", toolCalls.Count);
            return await _toolExecutor.ExecuteParallelAsync(toolCalls, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Executing {Count} tools sequentially", toolCalls.Count);
            return await _toolExecutor.ExecuteSequentialAsync(toolCalls, cancellationToken);
        }
    }

    /// <summary>
    /// Builds the default system prompt.
    /// </summary>
    private string BuildDefaultSystemPrompt()
    {
        return @"You are Foundz.Net, an expert AI coding assistant powered by Azure AI Foundry.

Your capabilities:
- Read, write, edit, and analyze code files
- Execute git operations
- Run shell commands and tests
- Search codebases and documentation
- Refactor and generate code
- Provide detailed explanations

Guidelines:
1. Always use tools to interact with the codebase - never make assumptions
2. Break complex tasks into smaller steps
3. Verify your changes by reading files after modifications
4. Ask for confirmation before destructive operations
5. Provide clear explanations of your actions
6. If you encounter errors, try alternative approaches
7. Be concise but thorough in your responses

You have access to 40+ tools across 8 categories. Use them effectively to accomplish user tasks.";
    }
}
