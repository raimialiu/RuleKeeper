using System.CommandLine;
using System.CommandLine.Invocation;
using RuleKeeper.Core.Rules;
using Spectre.Console;

namespace RuleKeeper.Cli.Commands;

public static class ExplainCommand
{
    public static Command Create()
    {
        var ruleIdArgument = new Argument<string>(
            name: "rule-id",
            description: "The rule ID to explain");

        var command = new Command("explain", "Explain a specific rule")
        {
            ruleIdArgument
        };

        command.SetHandler((InvocationContext context) =>
        {
            var ruleId = context.ParseResult.GetValueForArgument(ruleIdArgument);
            Execute(ruleId);
        });

        return command;
    }

    private static void Execute(string ruleId)
    {
        var registry = new RuleRegistry();

        // Register built-in rules
        registry.RegisterAssembly(typeof(RuleKeeper.Rules.Naming.ClassNamingAnalyzer).Assembly);

        var ruleInfo = registry.GetRuleInfo(ruleId);

        if (ruleInfo == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Rule not found: {ruleId}");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Run [bold]rulekeeper list-rules[/] to see available rules.");
            return;
        }

        // Create the analyzer to get additional information
        var analyzer = registry.CreateAnalyzer(ruleId);

        // Display rule information
        var panel = new Panel(new Markup($"[bold]{ruleInfo.Name}[/]"))
        {
            Header = new PanelHeader($"[blue]{ruleInfo.RuleId}[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow("[grey]Category:[/]", ruleInfo.Category);
        grid.AddRow("[grey]Default Severity:[/]", GetSeverityMarkup(ruleInfo.DefaultSeverity));
        grid.AddRow("[grey]Type:[/]", ruleInfo.Type.Name);

        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();

        if (!string.IsNullOrEmpty(ruleInfo.Description))
        {
            AnsiConsole.MarkupLine("[grey]Description:[/]");
            AnsiConsole.WriteLine(ruleInfo.Description);
            AnsiConsole.WriteLine();
        }

        // Show parameters if the analyzer has any
        var parameters = ruleInfo.Type.GetProperties()
            .Where(p => Attribute.IsDefined(p, typeof(RuleKeeper.Sdk.Attributes.RuleParameterAttribute)))
            .ToList();

        if (parameters.Count > 0)
        {
            AnsiConsole.MarkupLine("[grey]Parameters:[/]");
            var paramTable = new Table();
            paramTable.AddColumn("Name");
            paramTable.AddColumn("Type");
            paramTable.AddColumn("Default");
            paramTable.AddColumn("Description");

            foreach (var param in parameters)
            {
                var attr = (RuleKeeper.Sdk.Attributes.RuleParameterAttribute)
                    Attribute.GetCustomAttribute(param, typeof(RuleKeeper.Sdk.Attributes.RuleParameterAttribute))!;

                paramTable.AddRow(
                    attr.Name,
                    param.PropertyType.Name,
                    attr.DefaultValue?.ToString() ?? "-",
                    attr.Description ?? "-"
                );
            }

            AnsiConsole.Write(paramTable);
            AnsiConsole.WriteLine();
        }

        // Show configuration example
        AnsiConsole.MarkupLine("[grey]Configuration Example:[/]");
        var yaml = $@"coding_standards:
  {ruleInfo.Category}:
    rules:
      {ruleId.ToLower().Replace("-", "_")}:
        id: {ruleInfo.RuleId}
        enabled: true
        severity: {ruleInfo.DefaultSeverity.ToString().ToLower()}";

        AnsiConsole.Write(new Panel(yaml)
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(1)
        });
    }

    private static string GetSeverityMarkup(RuleKeeper.Sdk.SeverityLevel severity)
    {
        return severity switch
        {
            RuleKeeper.Sdk.SeverityLevel.Critical => "[red]Critical[/]",
            RuleKeeper.Sdk.SeverityLevel.High => "[orange1]High[/]",
            RuleKeeper.Sdk.SeverityLevel.Medium => "[yellow]Medium[/]",
            RuleKeeper.Sdk.SeverityLevel.Low => "[cyan]Low[/]",
            RuleKeeper.Sdk.SeverityLevel.Info => "[grey]Info[/]",
            _ => severity.ToString()
        };
    }
}
