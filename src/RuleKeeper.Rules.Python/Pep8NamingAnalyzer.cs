using System.Text.RegularExpressions;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Rules.Python;

/// <summary>
/// Python-specific rule that enforces PEP 8 naming conventions.
/// </summary>
[Rule("PY-NAME-001",
    Name = "PEP 8 Naming Conventions",
    Description = "Enforces Python PEP 8 naming conventions for functions, classes, and variables",
    Severity = SeverityLevel.Low,
    Category = "naming")]
[SupportedLanguages(Language.Python)]
public class Pep8NamingAnalyzer : BaseCrossLanguageRule, ILanguageSpecificRule
{
    /// <inheritdoc />
    public Language TargetLanguage => Language.Python;

    private static readonly Regex SnakeCaseRegex = new(@"^[a-z][a-z0-9_]*$", RegexOptions.Compiled);
    private static readonly Regex PascalCaseRegex = new(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Compiled);
    private static readonly Regex ConstantCaseRegex = new(@"^[A-Z][A-Z0-9_]*$", RegexOptions.Compiled);
    private static readonly Regex PrivateNameRegex = new(@"^_[a-z][a-z0-9_]*$", RegexOptions.Compiled);
    private static readonly Regex DunderRegex = new(@"^__[a-z][a-z0-9_]*__$", RegexOptions.Compiled);

    /// <summary>
    /// Whether to allow leading underscores for private members.
    /// </summary>
    [RuleParameter("allow_private_underscore", Description = "Allow single leading underscore for private members", DefaultValue = true)]
    public bool AllowPrivateUnderscore { get; set; } = true;

    /// <inheritdoc />
    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        // Check class names - should be PascalCase
        foreach (var classNode in context.GetClasses())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var className = ExtractName(classNode.Text, "class ");
            if (!string.IsNullOrEmpty(className) && !PascalCaseRegex.IsMatch(className))
            {
                yield return CreateViolation(
                    classNode,
                    $"Class '{className}' should use PascalCase (e.g., '{ToPascalCase(className)}')",
                    context,
                    $"Rename to {ToPascalCase(className)}"
                );
            }
        }

        // Check function/method names - should be snake_case
        foreach (var methodNode in context.GetMethods())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var methodName = ExtractFunctionName(methodNode.Text);
            if (!string.IsNullOrEmpty(methodName))
            {
                // Skip dunder methods (__init__, __str__, etc.)
                if (DunderRegex.IsMatch(methodName))
                    continue;

                // Check for snake_case (with optional leading underscore for private)
                bool isValid = SnakeCaseRegex.IsMatch(methodName);
                if (!isValid && AllowPrivateUnderscore)
                {
                    isValid = PrivateNameRegex.IsMatch(methodName);
                }

                if (!isValid)
                {
                    yield return CreateViolation(
                        methodNode,
                        $"Function '{methodName}' should use snake_case (e.g., '{ToSnakeCase(methodName)}')",
                        context,
                        $"Rename to {ToSnakeCase(methodName)}"
                    );
                }
            }
        }

        // Check for constants (ALL_CAPS) in module level
        foreach (var line in context.Root.Text.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.Contains(" = ") || trimmed.Contains("="))
            {
                var assignIndex = trimmed.IndexOf('=');
                if (assignIndex > 0)
                {
                    var varName = trimmed.Substring(0, assignIndex).Trim();
                    // Check if it looks like a constant (all caps with value)
                    if (ConstantCaseRegex.IsMatch(varName))
                    {
                        // This is fine - constants should be UPPER_CASE
                        continue;
                    }
                }
            }
        }
    }

    private static string ExtractName(string text, string prefix)
    {
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith(prefix))
            {
                var afterPrefix = trimmed.Substring(prefix.Length);
                var endIndex = afterPrefix.IndexOfAny(new[] { '(', ':', ' ' });
                return endIndex > 0 ? afterPrefix.Substring(0, endIndex).Trim() : afterPrefix.Trim();
            }
        }
        return "";
    }

    private static string ExtractFunctionName(string text)
    {
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("def ") || trimmed.StartsWith("async def "))
            {
                var prefix = trimmed.StartsWith("async") ? "async def " : "def ";
                var afterPrefix = trimmed.Substring(prefix.Length);
                var parenIndex = afterPrefix.IndexOf('(');
                return parenIndex > 0 ? afterPrefix.Substring(0, parenIndex).Trim() : afterPrefix.Trim();
            }
        }
        return "";
    }

    private static string ToPascalCase(string name)
    {
        var words = SplitIntoWords(name);
        return string.Join("", words.Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower()));
    }

    private static string ToSnakeCase(string name)
    {
        var words = SplitIntoWords(name);
        return string.Join("_", words.Select(w => w.ToLower()));
    }

    private static string[] SplitIntoWords(string name)
    {
        // Handle snake_case
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
}
