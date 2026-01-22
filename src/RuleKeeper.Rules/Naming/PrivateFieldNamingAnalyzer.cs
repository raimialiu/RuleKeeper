using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Naming;

/// <summary>
/// Ensures private fields follow naming conventions.
/// </summary>
[Rule("CS-NAME-004",
    Name = "Private Field Naming Convention",
    Description = "Private fields must use _camelCase naming",
    Severity = SeverityLevel.Low,
    Category = "naming_conventions")]
public class PrivateFieldNamingAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("pattern", Description = "Regex pattern for valid private field names", DefaultValue = @"^_[a-z][a-zA-Z0-9]*$")]
    public string Pattern { get; set; } = @"^_[a-z][a-zA-Z0-9]*$";

    [RuleParameter("require_underscore", Description = "Require underscore prefix", DefaultValue = true)]
    public bool RequireUnderscore { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var fields = root.DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .Where(f => f.Modifiers.Any(SyntaxKind.PrivateKeyword) ||
                       (!f.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                        !f.Modifiers.Any(SyntaxKind.ProtectedKeyword) &&
                        !f.Modifiers.Any(SyntaxKind.InternalKeyword)));

        var pattern = RequireUnderscore ? Pattern : @"^[a-z][a-zA-Z0-9]*$";
        var regex = new System.Text.RegularExpressions.Regex(pattern);

        foreach (var field in fields)
        {
            // Skip const fields
            if (field.Modifiers.Any(SyntaxKind.ConstKeyword))
                continue;

            // Skip static readonly fields (often treated as constants)
            if (field.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                continue;

            foreach (var variable in field.Declaration.Variables)
            {
                var name = variable.Identifier.Text;

                if (!regex.IsMatch(name))
                {
                    var hint = RequireUnderscore
                        ? $"Rename to '_{char.ToLower(name.TrimStart('_')[0])}{name.TrimStart('_').Substring(1)}'"
                        : $"Rename to start with lowercase letter";

                    yield return CreateViolation(
                        variable.Identifier.GetLocation(),
                        $"Private field '{name}' must use {(RequireUnderscore ? "_camelCase" : "camelCase")} naming",
                        context,
                        hint
                    );
                }
            }
        }
    }
}
