using Microsoft.CodeAnalysis;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace RuleKeeper.Sdk.CSharp;

/// <summary>
/// Base class for C#-specific rule analyzers that use Roslyn.
/// </summary>
public abstract class BaseCSharpRuleAnalyzer : ICSharpRuleAnalyzer, ILanguageSpecificRule
{
    private readonly RuleAttribute? _ruleAttribute;

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
    public Language TargetLanguage => Language.CSharp;

    /// <summary>
    /// Configuration parameters for this rule.
    /// </summary>
    protected Dictionary<string, object> Parameters { get; private set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseCSharpRuleAnalyzer"/> class.
    /// </summary>
    protected BaseCSharpRuleAnalyzer()
    {
        _ruleAttribute = (RuleAttribute?)Attribute.GetCustomAttribute(GetType(), typeof(RuleAttribute));
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
    public abstract IEnumerable<Violation> Analyze(CSharpAnalysisContext context);

    /// <summary>
    /// Creates a violation at the specified Roslyn location.
    /// </summary>
    /// <param name="location">The Roslyn location.</param>
    /// <param name="message">The violation message.</param>
    /// <param name="context">The analysis context.</param>
    /// <param name="fixHint">Optional fix hint.</param>
    /// <returns>A new violation.</returns>
    protected Violation CreateViolation(
        Location location,
        string message,
        CSharpAnalysisContext context,
        string? fixHint = null)
    {
        return FromRoslynLocation(
            location,
            RuleId,
            RuleName,
            context.CustomMessage ?? message,
            context.Severity,
            fixHint ?? context.FixHint
        );
    }

    /// <summary>
    /// Creates a violation at the specified Roslyn location with custom severity.
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
        return FromRoslynLocation(
            location,
            RuleId,
            RuleName,
            message,
            severity,
            fixHint
        );
    }

    /// <summary>
    /// Creates a Violation from a Roslyn Location.
    /// </summary>
    private static Violation FromRoslynLocation(
        Location location,
        string ruleId,
        string ruleName,
        string message,
        SeverityLevel severity,
        string? fixHint = null)
    {
        var lineSpan = location.GetLineSpan();
        var sourceTree = location.SourceTree;

        string? codeSnippet = null;
        if (sourceTree != null)
        {
            var text = sourceTree.GetText();
            var line = text.Lines[lineSpan.StartLinePosition.Line];
            codeSnippet = line.ToString();
        }

        return new Violation
        {
            RuleId = ruleId,
            RuleName = ruleName,
            Message = message,
            Severity = severity,
            Language = Language.CSharp,
            FilePath = lineSpan.Path ?? location.SourceTree?.FilePath ?? "unknown",
            StartLine = lineSpan.StartLinePosition.Line + 1,
            StartColumn = lineSpan.StartLinePosition.Character + 1,
            EndLine = lineSpan.EndLinePosition.Line + 1,
            EndColumn = lineSpan.EndLinePosition.Character + 1,
            FixHint = fixHint,
            CodeSnippet = codeSnippet
        };
    }

    /// <summary>
    /// Converts a Roslyn Location to a SourceLocation.
    /// </summary>
    /// <param name="location">The Roslyn location.</param>
    /// <returns>A SourceLocation.</returns>
    protected static SourceLocation ToSourceLocation(Location location)
    {
        var lineSpan = location.GetLineSpan();
        return new SourceLocation(
            lineSpan.Path ?? location.SourceTree?.FilePath ?? "unknown",
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.StartLinePosition.Character + 1,
            lineSpan.EndLinePosition.Line + 1,
            lineSpan.EndLinePosition.Character + 1,
            location.SourceSpan.Start,
            location.SourceSpan.End
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
