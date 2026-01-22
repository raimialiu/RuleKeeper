using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Sdk.Rules;

/// <summary>
/// Base class for cross-language rules that provides common functionality.
/// </summary>
public abstract class BaseCrossLanguageRule : ICrossLanguageRule
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
        _supportedLanguagesAttribute?.Languages ??
        new[] { Language.CSharp, Language.Python, Language.JavaScript, Language.TypeScript, Language.Java, Language.Go };

    /// <summary>
    /// Configuration parameters for this rule.
    /// </summary>
    protected Dictionary<string, object> Parameters { get; private set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseCrossLanguageRule"/> class.
    /// </summary>
    protected BaseCrossLanguageRule()
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

    /// <inheritdoc />
    public abstract IEnumerable<Violation> Analyze(UnifiedAnalysisContext context);

    /// <summary>
    /// Creates a violation at the specified node.
    /// </summary>
    /// <param name="node">The syntax node where the violation occurred.</param>
    /// <param name="message">The violation message.</param>
    /// <param name="context">The analysis context.</param>
    /// <param name="fixHint">Optional fix hint.</param>
    /// <returns>A new violation.</returns>
    protected Violation CreateViolation(
        IUnifiedSyntaxNode node,
        string message,
        UnifiedAnalysisContext context,
        string? fixHint = null)
    {
        return Violation.FromUnifiedNode(
            node,
            RuleId,
            RuleName,
            context.CustomMessage ?? message,
            context.Severity,
            fixHint ?? context.FixHint,
            context.SourceText
        );
    }

    /// <summary>
    /// Creates a violation at the specified location.
    /// </summary>
    /// <param name="location">The source location.</param>
    /// <param name="message">The violation message.</param>
    /// <param name="context">The analysis context.</param>
    /// <param name="fixHint">Optional fix hint.</param>
    /// <returns>A new violation.</returns>
    protected Violation CreateViolation(
        SourceLocation location,
        string message,
        UnifiedAnalysisContext context,
        string? fixHint = null)
    {
        string? codeSnippet = null;
        if (location.IsValid)
        {
            codeSnippet = context.GetLine(location.StartLine - 1);
        }

        return Violation.FromSourceLocation(
            location,
            RuleId,
            RuleName,
            context.CustomMessage ?? message,
            context.Severity,
            context.Language,
            fixHint ?? context.FixHint,
            codeSnippet
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

    /// <summary>
    /// Checks if the rule supports the specified language.
    /// </summary>
    /// <param name="language">The language to check.</param>
    /// <returns>True if the language is supported.</returns>
    public bool SupportsLanguage(Language language) => SupportedLanguages.Contains(language);
}
