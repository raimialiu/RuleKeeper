using System.CommandLine;
using RuleKeeper.Cli.Commands;

namespace RuleKeeper.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("RuleKeeper - A policy-as-code tool for C# using Roslyn")
        {
            Name = "rulekeeper"
        };

        // Add commands
        rootCommand.AddCommand(ScanCommand.Create());
        rootCommand.AddCommand(FixCommand.Create());
        rootCommand.AddCommand(InitCommand.Create());
        rootCommand.AddCommand(ValidateCommand.Create());
        rootCommand.AddCommand(ListRulesCommand.Create());
        rootCommand.AddCommand(ExplainCommand.Create());
        rootCommand.AddCommand(GenerateEditorConfigCommand.Create());
        rootCommand.AddCommand(GenerateAnalyzerConfigCommand.Create());

        return await rootCommand.InvokeAsync(args);
    }
}
