using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Sdk.Rules;

/// <summary>
/// Interface for rules that operate on the unified AST and can analyze code in multiple languages.
/// Cross-language rules use the language-agnostic <see cref="UnifiedAnalysisContext"/> for analysis.
/// </summary>
public interface ICrossLanguageRule
{
    /// <summary>
    /// Gets the unique identifier for this rule.
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Gets the display name for this rule.
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Gets the category this rule belongs to.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets a description of what this rule checks.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the default severity level for this rule.
    /// </summary>
    SeverityLevel DefaultSeverity { get; }

    /// <summary>
    /// Gets the languages this rule supports.
    /// </summary>
    IEnumerable<Language> SupportedLanguages { get; }

    /// <summary>
    /// Initializes the rule with configuration parameters.
    /// </summary>
    /// <param name="parameters">Configuration parameters for the rule.</param>
    void Initialize(Dictionary<string, object> parameters);

    /// <summary>
    /// Analyzes the given unified context and returns any violations found.
    /// </summary>
    /// <param name="context">The unified analysis context containing the syntax tree.</param>
    /// <returns>An enumerable of violations found.</returns>
    IEnumerable<Violation> Analyze(UnifiedAnalysisContext context);
}

/// <summary>
/// Marker interface for rules that are specific to a single language.
/// Language-specific rules may use native language features not available in the unified AST.
/// </summary>
public interface ILanguageSpecificRule
{
    /// <summary>
    /// Gets the programming language this rule is designed for.
    /// </summary>
    Language TargetLanguage { get; }
}

/// <summary>
/// Attribute to specify the languages a rule supports.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class SupportedLanguagesAttribute : Attribute
{
    /// <summary>
    /// Gets the supported languages.
    /// </summary>
    public Language[] Languages { get; }

    /// <summary>
    /// Initializes a new instance with the specified languages.
    /// </summary>
    /// <param name="languages">The supported languages.</param>
    public SupportedLanguagesAttribute(params Language[] languages)
    {
        Languages = languages;
    }
}
