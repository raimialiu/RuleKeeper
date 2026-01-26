using RuleKeeper.Sdk;

namespace RuleKeeper.Core.Validators;

/// <summary>
/// Interface for custom validators that can be defined in YAML or loaded from assemblies.
/// </summary>
public interface ICustomValidator
{
    /// <summary>
    /// Unique identifier for this validator.
    /// </summary>
    string ValidatorId { get; }

    /// <summary>
    /// Display name for the validator.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of what this validator checks.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Languages supported by this validator.
    /// Empty means all languages are supported.
    /// </summary>
    IReadOnlyList<Language> SupportedLanguages { get; }

    /// <summary>
    /// Initialize the validator with configuration parameters.
    /// </summary>
    /// <param name="parameters">Configuration parameters from YAML.</param>
    void Initialize(Dictionary<string, object> parameters);

    /// <summary>
    /// Validate source code and return any violations.
    /// </summary>
    /// <param name="context">The validation context containing source code and AST.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of violations found.</returns>
    Task<IEnumerable<Violation>> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if this validator supports the given language.
    /// </summary>
    bool SupportsLanguage(Language language);
}

/// <summary>
/// Base implementation of ICustomValidator with common functionality.
/// </summary>
public abstract class CustomValidatorBase : ICustomValidator
{
    public abstract string ValidatorId { get; }
    public abstract string Name { get; }
    public virtual string? Description => null;

    private List<Language> _supportedLanguages = new();
    public IReadOnlyList<Language> SupportedLanguages => _supportedLanguages;

    protected Dictionary<string, object> Parameters { get; private set; } = new();

    public virtual void Initialize(Dictionary<string, object> parameters)
    {
        Parameters = parameters ?? new Dictionary<string, object>();
    }

    public abstract Task<IEnumerable<Violation>> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default);

    public virtual bool SupportsLanguage(Language language)
    {
        return _supportedLanguages.Count == 0 || _supportedLanguages.Contains(language);
    }

    protected void SetSupportedLanguages(params Language[] languages)
    {
        _supportedLanguages = languages.ToList();
    }

    protected T GetParameter<T>(string key, T defaultValue)
    {
        if (Parameters.TryGetValue(key, out var value))
        {
            if (value is T typed)
                return typed;

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

    protected Violation CreateViolation(
        ValidationContext context,
        string message,
        int startLine,
        int startColumn,
        int endLine = 0,
        int endColumn = 0,
        string? codeSnippet = null)
    {
        return new Violation
        {
            RuleId = context.RuleId ?? ValidatorId,
            RuleName = context.RuleName ?? Name,
            Message = message,
            Severity = context.Severity,
            FilePath = context.FilePath,
            StartLine = startLine,
            StartColumn = startColumn,
            EndLine = endLine > 0 ? endLine : startLine,
            EndColumn = endColumn > 0 ? endColumn : startColumn,
            FixHint = context.FixHint,
            CodeSnippet = codeSnippet
        };
    }
}
