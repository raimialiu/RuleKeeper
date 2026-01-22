using Microsoft.CodeAnalysis;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Sdk;

/// <summary>
/// Base class for rule analyzers that provides common functionality.
/// This base class supports C# analysis using Roslyn.
/// </summary>
/// <remarks>
/// For cross-language rules, use <see cref="BaseCrossLanguageRule"/> instead.
/// </remarks>
public abstract class BaseRuleAnalyzer : IRuleAnalyzer, ILanguageSpecificRule
{
    private readonly RuleAttribute? _ruleAttribute;
    private readonly SupportedLanguagesAttribute? _supportedLanguagesAttribute;

    /// <inheritdoc />
    public virtual string RuleId => _ruleAttribute?.RuleId ?? GetType().Name;

    /// <inheritdoc />
    public virtual string RuleName => _ruleAttribute?.Name ?? GetType().Name;

    /// <inheritdoc />
    public virtual string Category => _ruleAttribute?.Category ?? "General";

    /// <inheritdoc />
    public virtual string Description => _ruleAttribute?.Description ?? "";

    /// <inheritdoc />
    public virtual SeverityLevel DefaultSeverity => _ruleAttribute?.Severity ?? SeverityLevel.Medium;

    /// <inheritdoc />
    public virtual IEnumerable<Language> SupportedLanguages =>
        _supportedLanguagesAttribute?.Languages ?? new[] { Language.CSharp };

    /// <summary>
    /// Gets the target language for this rule. For backward compatibility, defaults to C#.
    /// </summary>
    public Language TargetLanguage => Language.CSharp;

    /// <summary>
    /// Configuration parameters for this rule.
    /// </summary>
    protected Dictionary<string, object> Parameters { get; private set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRuleAnalyzer"/> class.
    /// </summary>
    protected BaseRuleAnalyzer()
    {
        _ruleAttribute = (RuleAttribute?)Attribute.GetCustomAttribute(GetType(), typeof(RuleAttribute));
        _supportedLanguagesAttribute = (SupportedLanguagesAttribute?)Attribute.GetCustomAttribute(GetType(), typeof(SupportedLanguagesAttribute));
    }

    /// <inheritdoc />
    public virtual void Initialize(Dictionary<string, object> parameters)
    {
        Parameters = parameters ?? new Dictionary<string, object>();
        ApplyParameterAttributes();
    }

    /// <summary>
    /// Applies RuleParameterAttribute values from the parameters dictionary.
    /// </summary>
    private void ApplyParameterAttributes()
    {
        var properties = GetType().GetProperties()
            .Where(p => Attribute.IsDefined(p, typeof(RuleParameterAttribute)));

        foreach (var property in properties)
        {
            var attr = (RuleParameterAttribute)Attribute.GetCustomAttribute(property, typeof(RuleParameterAttribute))!;

            if (Parameters.TryGetValue(attr.Name, out var value))
            {
                try
                {
                    var convertedValue = Convert.ChangeType(value, property.PropertyType);
                    property.SetValue(this, convertedValue);
                }
                catch
                {
                    if (attr.DefaultValue != null)
                    {
                        property.SetValue(this, attr.DefaultValue);
                    }
                }
            }
            else if (attr.DefaultValue != null)
            {
                property.SetValue(this, attr.DefaultValue);
            }
        }
    }

    /// <summary>
    /// Analyzes the given context and returns any violations found.
    /// </summary>
    /// <param name="context">The analysis context containing the syntax tree and semantic model.</param>
    /// <returns>Enumerable of violations found.</returns>
    public abstract IEnumerable<Violation> Analyze(AnalysisContext context);

    /// <summary>
    /// Creates a violation at the specified location.
    /// </summary>
    /// <param name="location">The Roslyn location.</param>
    /// <param name="message">The violation message.</param>
    /// <param name="context">The analysis context.</param>
    /// <param name="fixHint">Optional fix hint.</param>
    /// <returns>A new violation.</returns>
    protected Violation CreateViolation(
        Location location,
        string message,
        AnalysisContext context,
        string? fixHint = null)
    {
        return Violation.FromLocation(
            location,
            RuleId,
            RuleName,
            context.CustomMessage ?? message,
            context.Severity,
            fixHint ?? context.FixHint
        );
    }

    /// <summary>
    /// Creates a violation at the specified location with custom severity.
    /// </summary>
    /// <param name="location">The Roslyn location.</param>
    /// <param name="message">The violation message.</param>
    /// <param name="severity">The severity level.</param>
    /// <param name="fixHint">Optional fix hint.</param>
    /// <returns>A new violation.</returns>
    protected Violation CreateViolation(
        Location location,
        string message,
        SeverityLevel severity,
        string? fixHint = null)
    {
        return Violation.FromLocation(
            location,
            RuleId,
            RuleName,
            message,
            severity,
            fixHint
        );
    }

    /// <summary>
    /// Creates a violation from a unified syntax node.
    /// </summary>
    /// <param name="node">The unified syntax node.</param>
    /// <param name="message">The violation message.</param>
    /// <param name="context">The analysis context.</param>
    /// <param name="fixHint">Optional fix hint.</param>
    /// <returns>A new violation.</returns>
    protected Violation CreateViolation(
        IUnifiedSyntaxNode node,
        string message,
        AnalysisContext context,
        string? fixHint = null)
    {
        return Violation.FromSourceLocation(
            node.Location,
            RuleId,
            RuleName,
            context.CustomMessage ?? message,
            context.Severity,
            node.Language,
            fixHint ?? context.FixHint
        );
    }

    /// <summary>
    /// Gets a parameter value with a default fallback.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    /// <param name="name">The parameter name.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The parameter value or default.</returns>
    protected T GetParameter<T>(string name, T defaultValue)
    {
        if (Parameters.TryGetValue(name, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Checks if a parameter exists.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <returns>True if the parameter exists.</returns>
    protected bool HasParameter(string name) => Parameters.ContainsKey(name);
}
