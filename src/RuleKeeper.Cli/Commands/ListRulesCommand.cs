using System.CommandLine;
using System.CommandLine.Invocation;
using RuleKeeper.Core.Rules;
using RuleKeeper.Sdk;
using Spectre.Console;

namespace RuleKeeper.Cli.Commands;

public static class ListRulesCommand
{
    public static Command Create()
    {
        var categoryOption = new Option<string?>(
            aliases: new[] { "--category", "-c" },
            description: "Filter by category");

        var formatOption = new Option<string>(
            aliases: new[] { "--format", "-f" },
            description: "Output format (table, json, markdown)",
            getDefaultValue: () => "table");

        var command = new Command("list-rules", "List all available rules")
        {
            categoryOption,
            formatOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            var category = context.ParseResult.GetValueForOption(categoryOption);
            var format = context.ParseResult.GetValueForOption(formatOption);

            Execute(category, format);
        });

        return command;
    }

    private static void Execute(string? category, string format)
    {
        var registry = new RuleRegistry();

        // Register built-in rules
        registry.RegisterAssembly(typeof(RuleKeeper.Rules.Naming.ClassNamingAnalyzer).Assembly);

        var rules = string.IsNullOrEmpty(category)
            ? registry.Rules.ToList()
            : registry.GetRulesByCategory(category).ToList();

        if (rules.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No rules found.[/]");
            return;
        }

        switch (format.ToLowerInvariant())
        {
            case "json":
                OutputJson(rules);
                break;
            case "markdown":
                OutputMarkdown(rules);
                break;
            default:
                OutputTable(rules);
                break;
        }
    }

    private static void OutputTable(List<RuleInfo> rules)
    {
        var table = new Table();
        table.AddColumn("Rule ID");
        table.AddColumn("Name");
        table.AddColumn("Category");
        table.AddColumn("Severity");
        table.AddColumn("Description");

        foreach (var rule in rules.OrderBy(r => r.Category).ThenBy(r => r.RuleId))
        {
            var severityColor = rule.DefaultSeverity switch
            {
                SeverityLevel.Critical => "red",
                SeverityLevel.High => "orange1",
                SeverityLevel.Medium => "yellow",
                SeverityLevel.Low => "cyan",
                SeverityLevel.Info => "grey",
                _ => "white"
            };

            table.AddRow(
                $"[bold]{rule.RuleId}[/]",
                rule.Name,
                rule.Category,
                $"[{severityColor}]{rule.DefaultSeverity}[/]",
                Truncate(rule.Description, 40)
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Total: [bold]{rules.Count}[/] rules");
    }

    private static void OutputJson(List<RuleInfo> rules)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            rules.Select(r => new
            {
                r.RuleId,
                r.Name,
                r.Category,
                Severity = r.DefaultSeverity.ToString(),
                r.Description
            }),
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        Console.WriteLine(json);
    }

    private static void OutputMarkdown(List<RuleInfo> rules)
    {
        Console.WriteLine("# RuleKeeper Rules");
        Console.WriteLine();

        var categories = rules.GroupBy(r => r.Category).OrderBy(g => g.Key);

        foreach (var category in categories)
        {
            Console.WriteLine($"## {category.Key}");
            Console.WriteLine();
            Console.WriteLine("| Rule ID | Name | Severity | Description |");
            Console.WriteLine("|---------|------|----------|-------------|");

            foreach (var rule in category.OrderBy(r => r.RuleId))
            {
                Console.WriteLine($"| `{rule.RuleId}` | {rule.Name} | {rule.DefaultSeverity} | {rule.Description} |");
            }

            Console.WriteLine();
        }
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        return text.Length <= maxLength
            ? text
            : text.Substring(0, maxLength - 3) + "...";
    }
}
