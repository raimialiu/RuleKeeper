using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Design;

/// <summary>
/// Enforces the "one class per file" coding standard.
/// Multiple classes per file make code harder to navigate and maintain.
/// </summary>
[Rule("CS-DESIGN-012",
    Name = "Multiple Classes Per File",
    Description = "Each file should contain only one class",
    Severity = SeverityLevel.Low,
    Category = "design")]
public class MultipleClassesPerFileAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("allow_nested", Description = "Allow nested classes", DefaultValue = true)]
    public bool AllowNested { get; set; } = true;

    [RuleParameter("allow_private", Description = "Allow private classes in same file", DefaultValue = true)]
    public bool AllowPrivate { get; set; } = true;

    [RuleParameter("max_classes", Description = "Maximum number of top-level classes per file", DefaultValue = 1)]
    public int MaxClasses { get; set; } = 1;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);

        // Get all top-level type declarations (not nested)
        var topLevelTypes = root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Where(t => t.Parent is NamespaceDeclarationSyntax ||
                        t.Parent is FileScopedNamespaceDeclarationSyntax ||
                        t.Parent is CompilationUnitSyntax);

        var classes = topLevelTypes
            .OfType<ClassDeclarationSyntax>()
            .ToList();

        // Filter out private classes if allowed
        if (AllowPrivate)
        {
            classes = classes
                .Where(c => c.Modifiers.Any(m => m.Text == "public" ||
                                                  m.Text == "internal" ||
                                                  m.Text == "protected"))
                .ToList();
        }

        if (classes.Count > MaxClasses)
        {
            // Report on all classes after the first one
            foreach (var classDecl in classes.Skip(MaxClasses))
            {
                yield return CreateViolation(
                    classDecl.Identifier.GetLocation(),
                    $"File contains multiple classes. Class '{classDecl.Identifier.Text}' should be in its own file",
                    context,
                    $"Move '{classDecl.Identifier.Text}' to a file named '{classDecl.Identifier.Text}.cs'"
                );
            }
        }
    }
}
