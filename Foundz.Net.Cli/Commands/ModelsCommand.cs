using Foundz.Net.Core.AI;
using Foundz.Net.Shared.Models;
using Spectre.Console;

namespace Foundz.Net.Cli.Commands;

/// <summary>
/// Command to list available models and their capabilities.
/// </summary>
public class ModelsCommand
{
    private readonly ModelCapabilityDetector _capabilityDetector;

    public ModelsCommand(ModelCapabilityDetector capabilityDetector)
    {
        _capabilityDetector = capabilityDetector;
    }

    /// <summary>
    /// Executes the models command to list all known models.
    /// </summary>
    public void Execute()
    {
        var rule = new Rule("[bold blue]🤖 Available AI Models[/]");
        rule.LeftJustified();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // Group models by provider
        var models = GetKnownModels();

        foreach (var providerGroup in models.GroupBy(m => m.Provider))
        {
            AnsiConsole.MarkupLine($"[bold yellow]{providerGroup.Key}[/]");
            AnsiConsole.WriteLine();

            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("[bold]Model[/]");
            table.AddColumn("[bold]Tool Use[/]");
            table.AddColumn("[bold]Vision[/]");
            table.AddColumn("[bold]Streaming[/]");
            table.AddColumn("[bold]JSON Mode[/]");
            table.AddColumn("[bold]Context[/]", c => c.RightAligned());
            table.AddColumn("[bold]Cost/1K In[/]", c => c.RightAligned());
            table.AddColumn("[bold]Cost/1K Out[/]", c => c.RightAligned());

            foreach (var model in providerGroup.OrderBy(m => m.ModelName))
            {
                table.AddRow(
                    model.ModelName,
                    FormatBool(model.SupportsToolUse),
                    FormatBool(model.SupportsVision),
                    FormatBool(model.SupportsStreaming),
                    FormatBool(model.SupportsJsonMode),
                    $"{model.MaxContextTokens / 1000}K",
                    $"${model.CostPer1kInputTokens:F2}",
                    $"${model.CostPer1kOutputTokens:F2}"
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Executes the models command for a specific model.
    /// </summary>
    public void ExecuteForModel(string modelName)
    {
        var capabilities = _capabilityDetector.GetCapabilities(modelName);

        var rule = new Rule($"[bold blue]🤖 Model Details: {modelName}[/]");
        rule.LeftJustified();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // Basic info
        var infoTable = new Table();
        infoTable.Border(TableBorder.Rounded);
        infoTable.AddColumn("[bold]Property[/]");
        infoTable.AddColumn("[bold]Value[/]");

        infoTable.AddRow("Model Name", capabilities.ModelName);
        infoTable.AddRow("Provider", capabilities.Provider);
        infoTable.AddRow("Tool Use", FormatBool(capabilities.SupportsToolUse));
        infoTable.AddRow("Vision/Multimodal", FormatBool(capabilities.SupportsVision));
        infoTable.AddRow("Streaming", FormatBool(capabilities.SupportsStreaming));
        infoTable.AddRow("JSON Mode", FormatBool(capabilities.SupportsJsonMode));
        infoTable.AddRow("Parallel Tool Calls", FormatBool(capabilities.SupportsParallelToolCalls));
        infoTable.AddRow("Max Context Tokens", $"{capabilities.MaxContextTokens:N0}");
        infoTable.AddRow("Max Output Tokens", $"{capabilities.MaxOutputTokens:N0}");

        AnsiConsole.Write(infoTable);
        AnsiConsole.WriteLine();

        // Pricing
        var pricingTable = new Table();
        pricingTable.Border(TableBorder.Rounded);
        pricingTable.AddColumn("[bold]Token Type[/]");
        pricingTable.AddColumn("[bold]Cost per 1K Tokens[/]");
        pricingTable.AddColumn("[bold]Cost per 1M Tokens[/]");

        pricingTable.AddRow(
            "Input (Prompt)",
            $"${capabilities.CostPer1kInputTokens:F4}",
            $"${capabilities.CostPer1kInputTokens * 1000:F2}"
        );
        pricingTable.AddRow(
            "Output (Completion)",
            $"${capabilities.CostPer1kOutputTokens:F4}",
            $"${capabilities.CostPer1kOutputTokens * 1000:F2}"
        );

        AnsiConsole.Write(pricingTable);
        AnsiConsole.WriteLine();

        // Features
        if (capabilities.Features.Any())
        {
            AnsiConsole.MarkupLine("[bold]Features:[/]");
            foreach (var feature in capabilities.Features)
            {
                AnsiConsole.MarkupLine($"  • {feature}");
            }
            AnsiConsole.WriteLine();
        }

        // Supported languages
        if (capabilities.SupportedLanguages.Any())
        {
            AnsiConsole.MarkupLine("[bold]Supported Languages:[/]");
            AnsiConsole.MarkupLine($"  {string.Join(", ", capabilities.SupportedLanguages)}");
        }
    }

    /// <summary>
    /// Compares multiple models.
    /// </summary>
    public void Compare(params string[] modelNames)
    {
        if (modelNames.Length < 2)
        {
            AnsiConsole.MarkupLine("[red]Please specify at least 2 models to compare.[/]");
            return;
        }

        var rule = new Rule("[bold blue]🤖 Model Comparison[/]");
        rule.LeftJustified();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]Feature[/]");
        
        foreach (var modelName in modelNames)
        {
            table.AddColumn($"[bold]{modelName}[/]");
        }

        var capabilities = modelNames.Select(m => _capabilityDetector.GetCapabilities(m)).ToList();

        // Comparison rows
        table.AddRow(PrependFeature("Provider", capabilities.Select(c => c.Provider)));
        table.AddRow(PrependFeature("Tool Use", capabilities.Select(c => FormatBool(c.SupportsToolUse))));
        table.AddRow(PrependFeature("Vision", capabilities.Select(c => FormatBool(c.SupportsVision))));
        table.AddRow(PrependFeature("Streaming", capabilities.Select(c => FormatBool(c.SupportsStreaming))));
        table.AddRow(PrependFeature("JSON Mode", capabilities.Select(c => FormatBool(c.SupportsJsonMode))));
        table.AddRow(PrependFeature("Context", capabilities.Select(c => $"{c.MaxContextTokens / 1000}K")));
        table.AddRow(PrependFeature("Max Output", capabilities.Select(c => $"{c.MaxOutputTokens / 1000}K")));
        table.AddRow(PrependFeature("Cost/1K In", capabilities.Select(c => $"${c.CostPer1kInputTokens:F2}")));
        table.AddRow(PrependFeature("Cost/1K Out", capabilities.Select(c => $"${c.CostPer1kOutputTokens:F2}")));

        AnsiConsole.Write(table);
    }

    private string[] PrependFeature(string featureName, IEnumerable<string> values)
    {
        return new[] { featureName }.Concat(values).ToArray();
    }

    private string FormatBool(bool value)
    {
        return value ? "[green]✓[/]" : "[grey]✗[/]";
    }

    private List<ModelCapabilities> GetKnownModels()
    {
        var models = new List<string>
        {
            // Anthropic
            "claude-3-5-sonnet-20241022",
            "claude-3-opus-20240229",
            "claude-3-sonnet-20240229",
            "claude-3-haiku-20240307",

            // OpenAI
            "gpt-4-turbo",
            "gpt-4o",
            "gpt-4",
            "gpt-3.5-turbo",

            // Mistral
            "mistral-large-latest",
            "mistral-medium-latest",
            "mistral-small-latest",

            // Cohere
            "command-r-plus",
            "command-r",

            // Meta
            "llama-3.1-405b",
            "llama-3.1-70b",
            "llama-3.1-8b"
        };

        return models.Select(m => _capabilityDetector.GetCapabilities(m)).ToList();
    }
}
