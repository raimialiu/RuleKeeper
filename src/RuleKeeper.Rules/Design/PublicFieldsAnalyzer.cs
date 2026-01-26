using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Detects public fields that should be properties.
/// Public fields break encapsulation and prevent future changes.
/// </summary>
[Rule("CS-DESIGN-013",
    Name = "Public Fields",
    Description = "Public fields should be properties instead",
    Severity = SeverityLevel.Medium,
    Category = "design")]
public class PublicFieldsAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("allow_const", Description = "Allow public const fields", DefaultValue = true)]
    public bool AllowConst { get; set; } = true;

    [RuleParameter("allow_readonly", Description = "Allow public readonly fields", DefaultValue = true)]
    public bool AllowReadonly { get; set; } = true;

    [RuleParameter("allow_static_readonly", Description = "Allow public static readonly fields", DefaultValue = true)]
    public bool AllowStaticReadonly { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        var fields = root.DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .Where(f => f.Modifiers.Any(SyntaxKind.PublicKeyword));

        foreach (var field in fields)
        {
            // Skip const if allowed
            if (AllowConst && field.Modifiers.Any(SyntaxKind.ConstKeyword))
                continue;

            // Skip readonly if allowed
            var isReadonly = field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);
            var isStatic = field.Modifiers.Any(SyntaxKind.StaticKeyword);

            if (AllowReadonly && isReadonly && !isStatic)
                continue;

            if (AllowStaticReadonly && isReadonly && isStatic)
                continue;

            foreach (var variable in field.Declaration.Variables)
            {
                var typeName = field.Declaration.Type.ToString();
                yield return CreateViolation(
                    variable.GetLocation(),
                    $"Public field '{variable.Identifier.Text}' should be a property",
                    context,
                    $"Convert to: public {typeName} {variable.Identifier.Text} {{ get; set; }}"
                );
            }
        }
    }
}
