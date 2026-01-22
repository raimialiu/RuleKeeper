namespace RuleKeeper.Sdk.Attributes;

/// <summary>
/// Defines a configurable parameter for a rule.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class RuleParameterAttribute : Attribute
{
    /// <summary>
    /// The name of the parameter in configuration.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// A description of the parameter.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The default value for the parameter.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Whether this parameter is required.
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>
    /// Creates a new RuleParameterAttribute.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    public RuleParameterAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
