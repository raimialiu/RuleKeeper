using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Rules.Python;

/// <summary>
/// Python-specific rule that checks for missing docstrings.
/// </summary>
[Rule("PY-DOC-001",
    Name = "Missing Docstring",
    Description = "Public functions and classes should have docstrings",
    Severity = SeverityLevel.Low,
    Category = "documentation")]
[SupportedLanguages(Language.Python)]
public class MissingDocstringAnalyzer : BaseCrossLanguageRule, ILanguageSpecificRule
{
    /// <inheritdoc />
    public Language TargetLanguage => Language.Python;

    /// <summary>
    /// Whether to require docstrings for public classes.
    /// </summary>
    [RuleParameter("require_class_docstring", Description = "Require docstrings for classes", DefaultValue = true)]
    public bool RequireClassDocstring { get; set; } = true;

    /// <summary>
    /// Whether to require docstrings for public functions.
    /// </summary>
    [RuleParameter("require_function_docstring", Description = "Require docstrings for functions", DefaultValue = true)]
    public bool RequireFunctionDocstring { get; set; } = true;

    /// <summary>
    /// Minimum function length before requiring a docstring.
    /// </summary>
    [RuleParameter("min_function_length", Description = "Minimum lines before requiring docstring", DefaultValue = 5)]
    public int MinFunctionLength { get; set; } = 5;

    /// <summary>
    /// Whether to skip private functions (starting with _).
    /// </summary>
    [RuleParameter("skip_private", Description = "Skip private functions and methods", DefaultValue = true)]
    public bool SkipPrivate { get; set; } = true;

    /// <inheritdoc />
    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        // Check classes for docstrings
        if (RequireClassDocstring)
        {
            foreach (var classNode in context.GetClasses())
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var className = ExtractClassName(classNode.Text);
                if (SkipPrivate && className.StartsWith("_"))
                    continue;

                if (!HasDocstring(classNode.Text))
                {
                    yield return CreateViolation(
                        classNode,
                        $"Class '{className}' is missing a docstring",
                        context,
                        "Add a docstring describing the class purpose"
                    );
                }
            }
        }

        // Check functions for docstrings
        if (RequireFunctionDocstring)
        {
            foreach (var methodNode in context.GetMethods())
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var methodName = ExtractFunctionName(methodNode.Text);

                // Skip private methods if configured
                if (SkipPrivate && methodName.StartsWith("_") && !methodName.StartsWith("__"))
                    continue;

                // Skip dunder methods
                if (methodName.StartsWith("__") && methodName.EndsWith("__"))
                    continue;

                // Check function length
                var lines = methodNode.Text.Split('\n');
                if (lines.Length < MinFunctionLength)
                    continue;

                if (!HasDocstring(methodNode.Text))
                {
                    yield return CreateViolation(
                        methodNode,
                        $"Function '{methodName}' is missing a docstring",
                        context,
                        "Add a docstring describing the function purpose, parameters, and return value"
                    );
                }
            }
        }
    }

    private static bool HasDocstring(string text)
    {
        var lines = text.Split('\n');
        if (lines.Length < 2)
            return false;

        // Find the first non-empty line after the definition
        bool foundDefinition = false;
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            if (!foundDefinition)
            {
                if (trimmed.StartsWith("def ") || trimmed.StartsWith("async def ") ||
                    trimmed.StartsWith("class "))
                {
                    foundDefinition = true;
                }
                continue;
            }

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Check for docstring
            if (trimmed.StartsWith("\"\"\"") || trimmed.StartsWith("'''"))
                return true;

            // If we hit a non-docstring statement, no docstring exists
            return false;
        }

        return false;
    }

    private static string ExtractClassName(string text)
    {
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("class "))
            {
                var afterClass = trimmed.Substring(6);
                var endIndex = afterClass.IndexOfAny(new[] { '(', ':', ' ' });
                return endIndex > 0 ? afterClass.Substring(0, endIndex).Trim() : afterClass.Trim();
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
}
