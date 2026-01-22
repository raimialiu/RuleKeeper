using Microsoft.CodeAnalysis;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Sdk;

/// <summary>
/// Base class for rule analyzers that provides common functionality.
/// </summary>
public abstract class BaseRuleAnalyzer : IRuleAnalyzer
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

    /// <summary>
    /// Configuration parameters for this rule.
    /// </summary>
    protected Dictionary<string, object> Parameters { get; private set; } = new();

    protected BaseRuleAnalyzer()
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
                    // Use default value if conversion fails
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
    public abstract IEnumerable<Violation> Analyze(AnalysisContext context);

    /// <summary>
    /// Creates a violation at the specified location.
    /// </summary>
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
    /// Gets a parameter value with a default fallback.
    /// </summary>
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
    protected bool HasParameter(string name) => Parameters.ContainsKey(name);
}
