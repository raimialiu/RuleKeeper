using System.CommandLine;
using System.CommandLine.Invocation;
using RuleKeeper.Core.Configuration;
using Spectre.Console;

namespace RuleKeeper.Cli.Commands;

public static class ValidateCommand
{
    public static Command Create()
    {
        var configArgument = new Argument<string>(
            name: "config",
            description: "Path to configuration file",
            getDefaultValue: () => "rulekeeper.yaml");

        var command = new Command("validate", "Validate a configuration file")
        {
            configArgument
        };

        command.SetHandler(async (InvocationContext context) =>
        {
            var configPath = context.ParseResult.GetValueForArgument(configArgument);
            var exitCode = await ExecuteAsync(configPath);
            context.ExitCode = exitCode;
        });

        return command;
    }

    private static Task<int> ExecuteAsync(string configPath)
    {
        try
        {
            var fullPath = Path.GetFullPath(configPath);

            if (!File.Exists(fullPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {configPath}");
                return Task.FromResult(1);
            }

            var loader = new ConfigurationLoader();
            var config = loader.LoadFromFile(fullPath);

            var validator = new ConfigurationValidator();
            var errors = validator.Validate(config);

            if (errors.Count == 0)
            {
                AnsiConsole.MarkupLine($"[green]Valid:[/] Configuration file is valid");

                // Show summary
                var ruleCount = config.CodingStandards.Values
                    .Sum(c => c.Rules.Count(r => r.IsEnabled));

                var policyCount = config.PrebuiltPolicies
                    .Count(p => p.Value.Enabled);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"  Custom rules: [bold]{ruleCount}[/]");
                AnsiConsole.MarkupLine($"  Pre-built policies: [bold]{policyCount}[/]");
                AnsiConsole.MarkupLine($"  Include patterns: [bold]{config.Scan.Include.Count}[/]");
                AnsiConsole.MarkupLine($"  Exclude patterns: [bold]{config.Scan.Exclude.Count}[/]");

                return Task.FromResult(0);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Invalid:[/] Configuration file has {errors.Count} error(s):");
                AnsiConsole.WriteLine();

                foreach (var error in errors)
                {
                    AnsiConsole.MarkupLine($"  [yellow]{error.Path}[/]: {error.Message}");
                }

                return Task.FromResult(1);
            }
        }
        catch (ConfigurationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return Task.FromResult(1);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return Task.FromResult(1);
        }
    }
}
