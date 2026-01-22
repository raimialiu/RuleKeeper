using System.Text.RegularExpressions;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Rules.CrossLanguage;

/// <summary>
/// Cross-language rule that checks naming conventions.
/// Works with all supported languages through the unified AST.
/// </summary>
[Rule("XL-NAME-001",
    Name = "Naming Conventions (Cross-Language)",
    Description = "Checks naming conventions for classes, methods, and variables",
    Severity = SeverityLevel.Low,
    Category = "naming")]
[SupportedLanguages(Language.CSharp, Language.Python, Language.JavaScript, Language.TypeScript, Language.Java, Language.Go)]
public class NamingConventionCrossLanguageAnalyzer : BaseCrossLanguageRule
{
    private static readonly Regex PascalCaseRegex = new(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Compiled);
    private static readonly Regex CamelCaseRegex = new(@"^[a-z][a-zA-Z0-9]*$", RegexOptions.Compiled);
    private static readonly Regex SnakeCaseRegex = new(@"^[a-z][a-z0-9_]*$", RegexOptions.Compiled);
    private static readonly Regex ConstantCaseRegex = new(@"^[A-Z][A-Z0-9_]*$", RegexOptions.Compiled);

    /// <summary>
    /// Naming style for classes.
    /// </summary>
    [RuleParameter("class_style", Description = "Naming style for classes (pascal, camel, snake)", DefaultValue = "pascal")]
    public string ClassStyle { get; set; } = "pascal";

    /// <summary>
    /// Naming style for methods.
    /// </summary>
    [RuleParameter("method_style", Description = "Naming style for methods", DefaultValue = "auto")]
    public string MethodStyle { get; set; } = "auto";

    /// <summary>
    /// Naming style for variables.
    /// </summary>
    [RuleParameter("variable_style", Description = "Naming style for variables", DefaultValue = "auto")]
    public string VariableStyle { get; set; } = "auto";

    /// <inheritdoc />
    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        // Check classes
        foreach (var classNode in context.GetClasses())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var className = GetIdentifierName(classNode);
            if (!string.IsNullOrEmpty(className))
            {
                var expectedStyle = GetStyleForLanguage(ClassStyle, context.Language, "class");
                if (!MatchesStyle(className, expectedStyle))
                {
                    yield return CreateViolation(
                        classNode,
                        $"Class '{className}' should use {expectedStyle} naming convention",
                        context,
                        $"Rename to {SuggestName(className, expectedStyle)}"
                    );
                }
            }
        }

        // Check methods
        foreach (var methodNode in context.GetMethods())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var methodName = GetIdentifierName(methodNode);
            if (!string.IsNullOrEmpty(methodName))
            {
                var expectedStyle = GetStyleForLanguage(MethodStyle, context.Language, "method");
                if (!MatchesStyle(methodName, expectedStyle))
                {
                    yield return CreateViolation(
                        methodNode,
                        $"Method '{methodName}' should use {expectedStyle} naming convention",
                        context,
                        $"Rename to {SuggestName(methodName, expectedStyle)}"
                    );
                }
            }
        }
    }

    private string GetStyleForLanguage(string configuredStyle, Language language, string memberType)
    {
        if (configuredStyle != "auto")
            return configuredStyle;

        // Auto-detect based on language conventions
        return language switch
        {
            Language.CSharp or Language.Java => memberType switch
            {
                "class" => "pascal",
                "method" => "pascal",
                "variable" => "camel",
                "constant" => "pascal",
                _ => "pascal"
            },
            Language.Python => memberType switch
            {
                "class" => "pascal",
                "method" => "snake",
                "variable" => "snake",
                "constant" => "constant",
                _ => "snake"
            },
            Language.JavaScript or Language.TypeScript => memberType switch
            {
                "class" => "pascal",
                "method" => "camel",
                "variable" => "camel",
                "constant" => "constant",
                _ => "camel"
            },
            Language.Go => memberType switch
            {
                "class" => "pascal",
                "method" => "camel",
                "variable" => "camel",
                "constant" => "pascal",
                _ => "camel"
            },
            _ => "camel"
        };
    }

    private bool MatchesStyle(string name, string style)
    {
        // Skip common special names
        if (name.StartsWith("_") || name.StartsWith("$") || name.Length < 2)
            return true;

        return style.ToLower() switch
        {
            "pascal" => PascalCaseRegex.IsMatch(name),
            "camel" => CamelCaseRegex.IsMatch(name),
            "snake" => SnakeCaseRegex.IsMatch(name),
            "constant" => ConstantCaseRegex.IsMatch(name),
            _ => true
        };
    }

    private string SuggestName(string name, string targetStyle)
    {
        var words = SplitIntoWords(name);

        return targetStyle.ToLower() switch
        {
            "pascal" => string.Join("", words.Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower())),
            "camel" => string.Join("", words.Select((w, i) =>
                i == 0 ? w.ToLower() : char.ToUpper(w[0]) + w.Substring(1).ToLower())),
            "snake" => string.Join("_", words.Select(w => w.ToLower())),
            "constant" => string.Join("_", words.Select(w => w.ToUpper())),
            _ => name
        };
    }

    private string[] SplitIntoWords(string name)
    {
        // Handle snake_case and CONSTANT_CASE
        if (name.Contains('_'))
        {
            return name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        }

        // Handle camelCase and PascalCase
        var words = new List<string>();
        var currentWord = "";

        foreach (var c in name)
        {
            if (char.IsUpper(c) && currentWord.Length > 0)
            {
                words.Add(currentWord);
                currentWord = c.ToString();
            }
            else
            {
                currentWord += c;
            }
        }

        if (currentWord.Length > 0)
            words.Add(currentWord);

        return words.ToArray();
    }

    private string? GetIdentifierName(IUnifiedSyntaxNode node)
    {
        var identifier = node.FirstChildOfKind(UnifiedSyntaxKind.Identifier);
        return identifier?.Text;
    }
}
