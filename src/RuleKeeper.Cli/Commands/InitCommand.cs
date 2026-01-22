using System.CommandLine;
using System.CommandLine.Invocation;
using RuleKeeper.Core.Configuration;
using Spectre.Console;

namespace RuleKeeper.Cli.Commands;

public static class InitCommand
{
    public static Command Create()
    {
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output file name",
            getDefaultValue: () => "rulekeeper.yaml");

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Overwrite existing file");

        var command = new Command("init", "Create a new configuration file")
        {
            outputOption,
            forceOption
        };

        command.SetHandler(async (InvocationContext context) =>
        {
            var output = context.ParseResult.GetValueForOption(outputOption)!;
            var force = context.ParseResult.GetValueForOption(forceOption);

            var exitCode = await ExecuteAsync(output, force);
            context.ExitCode = exitCode;
        });

        return command;
    }

    private static Task<int> ExecuteAsync(string output, bool force)
    {
        try
        {
            var fullPath = Path.GetFullPath(output);

            if (File.Exists(fullPath) && !force)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] File already exists: {output}");
                AnsiConsole.MarkupLine("Use [bold]--force[/] to overwrite.");
                return Task.FromResult(1);
            }

            var content = ConfigurationLoader.GenerateDefaultConfig();
            File.WriteAllText(fullPath, content);

            AnsiConsole.MarkupLine($"[green]Created:[/] {output}");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Next steps:");
            AnsiConsole.MarkupLine("  1. Edit the configuration file to customize rules");
            AnsiConsole.MarkupLine("  2. Run [bold]rulekeeper scan .[/] to analyze your code");
            AnsiConsole.MarkupLine("  3. Run [bold]rulekeeper list-rules[/] to see available rules");

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return Task.FromResult(1);
        }
    }
}
