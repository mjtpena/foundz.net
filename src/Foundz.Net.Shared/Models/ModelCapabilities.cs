namespace Foundz.Net.Shared.Models;

/// <summary>
/// Represents the capabilities of an AI model.
/// </summary>
public record ModelCapabilities
{
    public required string ModelName { get; init; }
    public required string Provider { get; init; }
    public bool SupportsToolUse { get; init; }
    public bool SupportsVision { get; init; }
    public bool SupportsJsonMode { get; init; }
    public bool SupportsStreaming { get; init; }
    public int MaxContextTokens { get; init; }
    public int MaxOutputTokens { get; init; }
    public decimal CostPer1kInputTokens { get; init; }
    public decimal CostPer1kOutputTokens { get; init; }
}
