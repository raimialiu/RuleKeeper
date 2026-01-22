using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Naming;

/// <summary>
/// Ensures class names follow PascalCase convention.
/// </summary>
[Rule("CS-NAME-001",
    Name = "Class Naming Convention",
    Description = "Classes must use PascalCase naming",
    Severity = SeverityLevel.Medium,
    Category = "naming_conventions")]
public class ClassNamingAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("pattern", Description = "Regex pattern for valid class names", DefaultValue = @"^[A-Z][a-zA-Z0-9]*$")]
    public string Pattern { get; set; } = @"^[A-Z][a-zA-Z0-9]*$";

    [RuleParameter("allow_underscores", Description = "Allow underscores in class names", DefaultValue = false)]
    public bool AllowUnderscores { get; set; } = false;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var classes = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>();

        var pattern = AllowUnderscores ? @"^[A-Z][a-zA-Z0-9_]*$" : Pattern;
        var regex = new System.Text.RegularExpressions.Regex(pattern);

        foreach (var cls in classes)
        {
            var name = cls.Identifier.Text;

            if (!regex.IsMatch(name))
            {
                yield return CreateViolation(
                    cls.Identifier.GetLocation(),
                    $"Class '{name}' must use PascalCase naming convention",
                    context,
                    "Rename the class to use PascalCase (e.g., MyClassName)"
                );
            }
        }
    }
}
