namespace RuleKeeper.Sdk.Attributes;

/// <summary>
/// Marks a class as a RuleKeeper fix provider.
/// Classes marked with this attribute will be discovered and registered automatically.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class FixProviderAttribute : Attribute
{
    /// <summary>
    /// The display name for this fix provider.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// A description of what fixes this provider offers.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The category of fixes this provider handles (e.g., "Naming", "Security").
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// The priority of this provider. Higher values are processed first.
    /// Default is 0.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Creates a new FixProviderAttribute.
    /// </summary>
    public FixProviderAttribute()
    {
    }

    /// <summary>
    /// Creates a new FixProviderAttribute with a name.
    /// </summary>
    /// <param name="name">The display name for this fix provider.</param>
    public FixProviderAttribute(string name)
    {
        Name = name;
    }
}
