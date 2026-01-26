using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Code;

/// <summary>
/// Detects local variables that are declared but never used.
/// Unused variables clutter code and may indicate incomplete implementation.
/// </summary>
[Rule("CS-CODE-006",
    Name = "Unused Variables",
    Description = "Detects variables that are declared but never used",
    Severity = SeverityLevel.Medium,
    Category = "code")]
public class UnusedVariablesAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("ignore_underscore", Description = "Ignore variables starting with underscore", DefaultValue = true)]
    public bool IgnoreUnderscore { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);
        var semanticModel = context.SemanticModel;

        if (semanticModel == null)
            yield break;

        var localDeclarations = root.DescendantNodes()
            .OfType<LocalDeclarationStatementSyntax>();

        foreach (var declaration in localDeclarations)
        {
            foreach (var variable in declaration.Declaration.Variables)
            {
                var variableName = variable.Identifier.Text;

                // Skip discard patterns
                if (variableName == "_")
                    continue;

                // Skip underscore-prefixed if configured
                if (IgnoreUnderscore && variableName.StartsWith("_"))
                    continue;

                // Get the symbol for this variable
                var symbol = semanticModel.GetDeclaredSymbol(variable);
                if (symbol == null)
                    continue;

                // Find all references to this symbol in the containing method
                var containingMethod = variable.Ancestors()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault();

                if (containingMethod == null)
                    continue;

                var references = containingMethod.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(id => id.Identifier.Text == variableName)
                    .Where(id => id != variable.Identifier.Parent) // Exclude the declaration itself
                    .ToList();

                // Check if any reference actually refers to this symbol
                var hasUsage = references.Any(r =>
                {
                    var refSymbol = semanticModel.GetSymbolInfo(r).Symbol;
                    return SymbolEqualityComparer.Default.Equals(refSymbol, symbol);
                });

                if (!hasUsage)
                {
                    yield return CreateViolation(
                        variable.GetLocation(),
                        $"Variable '{variableName}' is declared but never used",
                        context,
                        "Remove the unused variable or use it"
                    );
                }
            }
        }
    }
}
