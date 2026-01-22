namespace RuleKeeper.Sdk;

/// <summary>
/// Defines the severity levels for rule violations.
/// </summary>
public enum SeverityLevel
{
    /// <summary>
    /// Informational message, no action required.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Minor issue that should be addressed.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Moderate issue that should be fixed.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Significant issue that must be addressed.
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical issue that must be fixed immediately.
    /// </summary>
    Critical = 4
}
